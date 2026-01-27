CREATE DATABASE AVIMS_DB;
GO
USE AVIMS_DB;
GO
CREATE TABLE Roles (
    RoleId INT IDENTITY PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE Users (
    UserId INT IDENTITY PRIMARY KEY,
    Username NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Email NVARCHAR(150),
    RoleId INT NOT NULL,
    IsLocked BIT DEFAULT 0,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
);

CREATE TABLE AuditLogs (
    LogId INT IDENTITY PRIMARY KEY,
    UserId INT,
    Action NVARCHAR(255),
    LogDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
CREATE TABLE Staff (
    StaffId INT IDENTITY PRIMARY KEY,
    UserId INT NOT NULL,
    FullName NVARCHAR(150),
    Phone NVARCHAR(20),
    Position NVARCHAR(100),
    IsActive BIT DEFAULT 1,
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
CREATE TABLE Customers (
    CustomerId INT IDENTITY PRIMARY KEY,
    UserId INT,
    CustomerName NVARCHAR(150),
    Address NVARCHAR(255),
    Phone NVARCHAR(20),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
CREATE TABLE VehicleModels (
    ModelId INT IDENTITY PRIMARY KEY,
    ModelName NVARCHAR(100),        -- Sedan, SUV, Hatchback
    VehicleClass NVARCHAR(50),      -- A, B, C, D
    Description NVARCHAR(255)
);
CREATE TABLE Vehicles (
    VehicleId INT IDENTITY PRIMARY KEY,
    CustomerId INT,
    ModelId INT,
    VehicleName NVARCHAR(100),
    VehicleVersion NVARCHAR(50),
    VehicleRate DECIMAL(18,2),
    BodyNumber NVARCHAR(100),
    EngineNumber NVARCHAR(100),
    VehicleNumber NVARCHAR(50),
    FOREIGN KEY (CustomerId) REFERENCES Customers(CustomerId),
    FOREIGN KEY (ModelId) REFERENCES VehicleModels(ModelId)
);
CREATE TABLE VehicleInspections (
    InspectionId INT IDENTITY PRIMARY KEY,
    VehicleId INT,
    StaffId INT,
    InspectionDate DATETIME,
    Status NVARCHAR(50),
    Result NVARCHAR(255),
    FOREIGN KEY (VehicleId) REFERENCES Vehicles(VehicleId),
    FOREIGN KEY (StaffId) REFERENCES Staff(StaffId)
);
CREATE TABLE Estimates (
    EstimateId INT IDENTITY PRIMARY KEY,
    CustomerId INT,
    VehicleId INT,
    EstimateAmount DECIMAL(18,2),
    Warranty NVARCHAR(100),
    PolicyType NVARCHAR(100),
    CreatedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (CustomerId) REFERENCES Customers(CustomerId),
    FOREIGN KEY (VehicleId) REFERENCES Vehicles(VehicleId)
);
CREATE TABLE Policies (
    PolicyId INT IDENTITY PRIMARY KEY,
    CustomerId INT,
    VehicleId INT,
    PolicyDate DATE,
    Duration INT,
    Warranty NVARCHAR(100),
    Status NVARCHAR(50),
    FOREIGN KEY (CustomerId) REFERENCES Customers(CustomerId),
    FOREIGN KEY (VehicleId) REFERENCES Vehicles(VehicleId)
);
CREATE TABLE Bills (
    BillId INT IDENTITY PRIMARY KEY,
    PolicyId INT,
    BillDate DATE,
    Amount DECIMAL(18,2),
    Paid BIT DEFAULT 0,
    FOREIGN KEY (PolicyId) REFERENCES Policies(PolicyId)
);
CREATE TABLE Claims (
    ClaimId INT IDENTITY PRIMARY KEY,
    PolicyId INT,
    AccidentPlace NVARCHAR(255),
    AccidentDate DATE,
    InsuredAmount DECIMAL(18,2),
    ClaimAmount DECIMAL(18,2),
    Status NVARCHAR(50),
    FOREIGN KEY (PolicyId) REFERENCES Policies(PolicyId)
);
CREATE TABLE Penalties (
    PenaltyId INT IDENTITY PRIMARY KEY,
    PolicyId INT,
    Reason NVARCHAR(255),
    Amount DECIMAL(18,2),
    Status NVARCHAR(50),
    FOREIGN KEY (PolicyId) REFERENCES Policies(PolicyId)
);
CREATE TABLE InsuranceCancellations (
    CancellationId INT IDENTITY PRIMARY KEY,
    PolicyId INT,
    CancelDate DATE,
    RefundAmount DECIMAL(18,2),
    FOREIGN KEY (PolicyId) REFERENCES Policies(PolicyId)
);
CREATE TABLE CompanyExpenses (
    ExpenseId INT IDENTITY PRIMARY KEY,
    ExpenseDate DATE,
    ExpenseType NVARCHAR(100),
    Amount DECIMAL(18,2)
);
CREATE TABLE Feedback (
    FeedbackId INT IDENTITY PRIMARY KEY,
    CustomerId INT,
    Content NVARCHAR(500),
    CreatedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (CustomerId) REFERENCES Customers(CustomerId)
);

CREATE TABLE Testimonials (
    TestimonialId INT IDENTITY PRIMARY KEY,
    CustomerId INT,
    Content NVARCHAR(500),
    Approved BIT DEFAULT 0,
    FOREIGN KEY (CustomerId) REFERENCES Customers(CustomerId)
);

CREATE TABLE Contacts (
    ContactId INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(150),
    Email NVARCHAR(150),
    Message NVARCHAR(500),
    CreatedDate DATETIME DEFAULT GETDATE()
);
