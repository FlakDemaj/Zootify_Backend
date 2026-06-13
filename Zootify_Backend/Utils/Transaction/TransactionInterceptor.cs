using System.Reflection;
using Castle.DynamicProxy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.DataAccess.DBContext;

namespace Zootify_Backend.Transaction
{
    /// <summary>
    /// Interceptor for handling transactional operations.
    /// Manages transactions by starting, committing, or rolling back as needed.
    /// </summary>
    public class TransactionInterceptor : IInterceptor
    {
        private ZootifyDbContext Context { get; }

        private IDbContextTransaction? DbContextTransaction { get; set; }

        private List<Func<Task>> AfterTransactionActions { get; } = [];

        public TransactionInterceptor(ZootifyDbContext context)
        {
            Context = context;
        }

        /// <summary>
        /// Intercepts a method invocation and manages transaction handling.
        /// </summary>
        /// <param name="invocation">The method invocation being intercepted.</param>
        public void Intercept(IInvocation invocation)
        {
            var method = GetTargetMethod(invocation);
            var attribute = method.GetCustomAttribute<TransactionAttribute>();
            
            // Skip interception if no transaction attribute is present
            // or if NoTransactionAttribute is present
            if (attribute == null)
            {
                invocation.Proceed();
                return;
            }

            var isReadonly = attribute.ReadOnly;

            // If a transaction is already active, proceed without starting a new one
            if (DbContextTransaction != null)
            {
                invocation.Proceed();
                return;
            }

            StartTransaction(invocation.Proceed);

            // Handle async methods appropriately
            if (IsAsyncMethod(method))
            {
                invocation.ReturnValue = InterceptAsync((dynamic)invocation.ReturnValue!, isReadonly);
            }
            else
            {
                RunAfter(isReadonly).Wait();
            }
        }

        /// <summary>
        /// Starts a new database transaction and executes the given function.
        /// Rolls back the transaction in case of failure.
        /// </summary>
        /// <param name="function">The action to execute within the transaction.</param>
        private void StartTransaction(Action function)
        {
            DbContextTransaction = Context.Database.BeginTransaction();
            Context.Database.SetCommandTimeout(120);

            try
            {
                function();
            }
            catch (Exception e)
            {
                RollbackTransaction();
                throw new ZootifyException(ErrorCode.DatabaseError, e);
            }
        }

        /// <summary>
        /// Rolls back the current transaction and disposes of it.
        /// </summary>
        private void RollbackTransaction()
        {
            try
            {
                DbContextTransaction?.Rollback();
            }
            finally
            {
                DbContextTransaction?.Dispose();
                DbContextTransaction = null;
            }
        }

        /// <summary>
        /// Executes post-transaction actions such as committing or rolling back.
        /// </summary>
        /// <param name="isReadonly">If <see langword="true"/>, the transaction is rolled back instead of committed.</param>
        private async Task RunAfter(bool isReadonly)
        {
            try
            {
                if (isReadonly)
                {
                    await (DbContextTransaction?.RollbackAsync() ?? Task.CompletedTask);
                }
                else
                {
                    await Context.SaveChangesAsync();
                    await (DbContextTransaction?.CommitAsync() ?? Task.CompletedTask);
                }
            }
            catch (Exception e)
            {
                throw new ZootifyException(ErrorCode.DatabaseError, e);
            }
            finally
            {
                try
                {
                    await (DbContextTransaction?.DisposeAsync() ?? ValueTask.CompletedTask);
                }
                catch
                {
                    // Ignore exceptions during disposal
                }

                DbContextTransaction = null;
            }

            await InvokeAfterTransactionActions();
        }

        /// <summary>
        /// Executes any registered post-transaction actions.
        /// </summary>
        private async Task InvokeAfterTransactionActions()
        {
            var actions = AfterTransactionActions.ToList();
            AfterTransactionActions.Clear();

            foreach (var action in actions)
            {
                try
                {
                    await action();
                }
                catch (ZootifyException e)
                {
                    RollbackTransaction();
                    throw new ZootifyException(e.ErrorCode, e);
                }
                catch (Exception)
                {
                    RollbackTransaction();
                    throw new ZootifyException(ErrorCode.DefaultErrorCode);
                }
            }
        }

        /// <summary>
        /// Handles asynchronous method interception and transaction management.
        /// </summary>
        /// <param name="task">The asynchronous task returned by the intercepted method.</param>
        /// <param name="isReadonly">If <see langword="true"/>, the transaction is rolled back instead of committed.</param>
        private async Task InterceptAsync(Task task, bool isReadonly)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (ZootifyException e)
            {
                RollbackTransaction();
                throw new ZootifyException(e.ErrorCode, e);
            }
            catch (Exception)
            {
                RollbackTransaction();
                throw new ZootifyException(ErrorCode.DefaultErrorCode);
            }

            await RunAfter(isReadonly);
        }

        /// <summary>
        /// Handles asynchronous method interception for methods returning a value.
        /// </summary>
        /// <param name="task">The asynchronous task returned by the intercepted method.</param>
        /// <param name="isReadonly">If <see langword="true"/>, the transaction is rolled back instead of committed.</param>
        /// <returns>The result produced by the intercepted method.</returns>
        private async Task<T> InterceptAsync<T>(Task<T> task, bool isReadonly)
        {
            T result;
            try
            {
                result = await task.ConfigureAwait(false);
            }
            catch (ZootifyException e)
            {
                RollbackTransaction();
                throw new ZootifyException(e.ErrorCode, e);
            }
            catch (Exception)
            {
                RollbackTransaction();
                throw new ZootifyException(ErrorCode.DefaultErrorCode);
            }

            await RunAfter(isReadonly);
            return result;
        }

        /// <summary>
        /// Determines whether a method is asynchronous.
        /// </summary>
        /// <param name="method">The method to inspect.</param>
        /// <returns><see langword="true"/> if the method returns <see cref="Task"/> or <see cref="ValueTask{T}"/>; otherwise <see langword="false"/>.</returns>
        private static bool IsAsyncMethod(MethodInfo method)
        {
            return typeof(Task).IsAssignableFrom(method.ReturnType)
                   || (method.ReturnType.IsGenericType
                       && method.ReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>));
        }

        /// <summary>
        /// Retrieves the target method from an invocation, resolving interface-to-implementation mapping.
        /// </summary>
        /// <param name="invocation">The intercepted invocation to inspect.</param>
        /// <returns>The concrete implementation <see cref="MethodInfo"/>, or the interface method if no match is found.</returns>
        private static MethodInfo GetTargetMethod(IInvocation invocation)
        {
            var interfaceMethod = invocation.Method;
            var targetType = invocation.TargetType;
            var targetMethod = targetType?.GetMethods()
                .FirstOrDefault(
                    m => m.Name == interfaceMethod.Name
                         && m.GetParameters()
                             .Select(p => p.ParameterType)
                             .SequenceEqual(
                                 interfaceMethod.GetParameters()
                                     .Select(p => p.ParameterType)));

            return targetMethod ?? interfaceMethod;
        }
    }
}
