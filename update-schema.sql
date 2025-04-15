-- Add new columns to the Users table
ALTER TABLE Users ADD COLUMN Bio TEXT NULL;
ALTER TABLE Users ADD COLUMN FavoriteColor TEXT NULL;
ALTER TABLE Users ADD COLUMN LastLoginDate TEXT NULL;
ALTER TABLE Users ADD COLUMN TotalPetsAdopted INTEGER NOT NULL DEFAULT 0;
ALTER TABLE Users ADD COLUMN TotalItemsPurchased INTEGER NOT NULL DEFAULT 0;
