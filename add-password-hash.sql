-- Add PasswordHash column to the Users table
ALTER TABLE Users ADD COLUMN PasswordHash TEXT NOT NULL DEFAULT '';
