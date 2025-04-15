-- Update the demo user's password hash
-- This sets the password to "password" using SHA-256 hash
UPDATE Users 
SET PasswordHash = 'XohImNooBHFR0OVvjcYpJ3NgPQ1qq73WKhHvch0VQtg=' 
WHERE Username = 'demo';
