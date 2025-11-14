-- Creating Table User (Customer or Admin) data
CREATE TABLE Users (
	Id INT PRIMARY KEY IDENTITY (1,1),
	Username NVARCHAR (100) NOT NULL,
	PasswordHAsh NVARCHAR (256) NOT NULL,
	Role NVARCHAR (20) NOT NULL -- 'Customer' or 'Admin'
);

--Inserting Predetermined Customer & Admin Login data into Table Users
INSERT INTO Users (Username, PasswordHAsh, Role)
VALUES
	('customer01', 'customerpass123', 'Customer'),
	('admin01', 'adminpass123', 'Admin');

	--Retrieving all records from Table Users 
	SELECT * 
	FROM Users;

--Creating Table Cart for shopping cart data
CREATE TABLE Cart (
		Id INT PRIMARY KEY IDENTITY,
		CustomerUsername NVARCHAR(100),
		ProductId NVARCHAR(100),
		Quantity INT
		);
		

-- Retrieving all records from Table Cart
SELECT *
FROM Cart;