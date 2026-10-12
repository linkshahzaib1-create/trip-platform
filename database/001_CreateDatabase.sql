/*
  TripPlatform — create database
  Run in SSMS against your SQL Server instance.
*/
IF DB_ID(N'TripPlatform') IS NULL
BEGIN
    CREATE DATABASE TripPlatform;
END
GO

USE TripPlatform;
GO
