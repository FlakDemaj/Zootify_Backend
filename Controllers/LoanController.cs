using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Data;
using Zootify_Backend.DTOs;
using Zootify_Backend.Models;

namespace Zootify_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoanController(ZootifyDbContext db) : ControllerBase
{
    // GET /api/Loan — alle Ausleihen zurückgeben
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LoanDto>>> GetAll()
    {
        var loans = await db.Loans
            .Include(l => l.Lender)
            .Include(l => l.Borrower)
            .ToListAsync();

        return Ok(loans.Select(MapToDto));
    }

    // GET /api/Loan/{id} — einzelne Ausleihe
    [HttpGet("{id}")]
    public async Task<ActionResult<LoanDto>> GetById(int id)
    {
        var loan = await db.Loans
            .Include(l => l.Lender)
            .Include(l => l.Borrower)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (loan is null)
            return NotFound();

        return Ok(MapToDto(loan));
    }

    // GET /api/Loan/specimen/{specimenId} — alle Ausleihen eines Fundes
    [HttpGet("specimen/{specimenId}")]
    public async Task<ActionResult<IEnumerable<LoanDto>>> GetBySpecimen(int specimenId)
    {
        var loans = await db.Loans
            .Include(l => l.Lender)
            .Include(l => l.Borrower)
            .Where(l => l.SpecimenId == specimenId)
            .ToListAsync();

        return Ok(loans.Select(MapToDto));
    }

    // GET /api/Loan/overdue — alle überfälligen Ausleihen
    [HttpGet("overdue")]
    public async Task<ActionResult<IEnumerable<LoanDto>>> GetOverdue()
    {
        var loans = await db.Loans
            .Include(l => l.Lender)
            .Include(l => l.Borrower)
            .Where(l => l.ReturnDate == null && l.DueDate < DateTime.UtcNow)
            .ToListAsync();

        return Ok(loans.Select(MapToDto));
    }

    // POST /api/Loan — neue Ausleihe anlegen
    [HttpPost]
    public async Task<ActionResult<LoanDto>> Create(CreateLoanDto dto)
    {
        if (!await db.Specimens.AnyAsync(s => s.Id == dto.SpecimenId))
            return BadRequest("Fund nicht gefunden.");

        if (!await db.Users.AnyAsync(u => u.Id == dto.LenderId))
            return BadRequest("Verleiher nicht gefunden.");

        if (dto.BorrowerId.HasValue && !await db.Users.AnyAsync(u => u.Id == dto.BorrowerId))
            return BadRequest("Entleiher nicht gefunden.");

        if (dto.BorrowerId == null && string.IsNullOrWhiteSpace(dto.BorrowerExternalName))
            return BadRequest("Entweder BorrowerId oder BorrowerExternalName muss angegeben werden.");

        if (dto.DueDate <= dto.LoanDate)
            return BadRequest("Fälligkeitsdatum muss nach dem Ausleihdatum liegen.");

        var loan = new Loan
        {
            SpecimenId           = dto.SpecimenId,
            LenderId             = dto.LenderId,
            BorrowerId           = dto.BorrowerId,
            BorrowerExternalName = dto.BorrowerExternalName,
            LoanDate             = dto.LoanDate,
            DueDate              = dto.DueDate,
            Notes                = dto.Notes
        };

        db.Loans.Add(loan);
        await db.SaveChangesAsync();

        await db.Entry(loan).Reference(l => l.Lender).LoadAsync();
        await db.Entry(loan).Reference(l => l.Borrower).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = loan.Id }, MapToDto(loan));
    }

    // PUT /api/Loan/{id}/return — Ausleihe als zurückgegeben markieren
    [HttpPut("{id}/return")]
    public async Task<ActionResult<LoanDto>> Return(int id)
    {
        var loan = await db.Loans
            .Include(l => l.Lender)
            .Include(l => l.Borrower)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (loan is null)
            return NotFound();

        if (loan.ReturnDate.HasValue)
            return Conflict("Ausleihe wurde bereits zurückgegeben.");

        loan.ReturnDate = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(MapToDto(loan));
    }

    // DELETE /api/Loan/{id} — Ausleihe löschen
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var loan = await db.Loans.FindAsync(id);

        if (loan is null)
            return NotFound();

        db.Loans.Remove(loan);
        await db.SaveChangesAsync();

        return NoContent();
    }

    private static LoanDto MapToDto(Loan l) => new()
    {
        Id                   = l.Id,
        SpecimenId           = l.SpecimenId,
        LenderId             = l.LenderId,
        LenderName           = $"{l.Lender.FirstName} {l.Lender.LastName}",
        BorrowerId           = l.BorrowerId,
        BorrowerName         = l.Borrower != null ? $"{l.Borrower.FirstName} {l.Borrower.LastName}" : null,
        BorrowerExternalName = l.BorrowerExternalName,
        LoanDate             = l.LoanDate,
        DueDate              = l.DueDate,
        ReturnDate           = l.ReturnDate,
        Notes                = l.Notes
    };
}
