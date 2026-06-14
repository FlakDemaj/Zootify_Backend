ALTER TABLE [zootify].[user_settings]
ADD send_push_notifications bit Not NULL default 1,
    send_email_notifications bit Not NULL default 1;