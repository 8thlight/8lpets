-- Remove Fullness column from the Pets table
PRAGMA foreign_keys=off;

BEGIN TRANSACTION;

-- Create a temporary table without the Fullness column
CREATE TABLE Pets_temp (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Species TEXT NOT NULL,
    Color TEXT NOT NULL,
    Happiness INTEGER NOT NULL DEFAULT 50,
    Hunger INTEGER NOT NULL DEFAULT 50,
    Health INTEGER NOT NULL DEFAULT 100,
    CreatedDate TEXT NOT NULL,
    LastFed TEXT NOT NULL,
    UserId INTEGER NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

-- Copy data from the original table to the temporary table
INSERT INTO Pets_temp (Id, Name, Species, Color, Happiness, Hunger, Health, CreatedDate, LastFed, UserId)
SELECT Id, Name, Species, Color, Happiness, Hunger, Health, CreatedDate, LastFed, UserId FROM Pets;

-- Drop the original table
DROP TABLE Pets;

-- Rename the temporary table to the original table name
ALTER TABLE Pets_temp RENAME TO Pets;

COMMIT;

PRAGMA foreign_keys=on;
