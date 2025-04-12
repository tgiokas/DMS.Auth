# Authentication microservice with Keycloak

## Overview

**Authentication** is a standalone microservice for authentication and authorization, built using .NET 9 and integrated with **Keycloak**. 
While it is part of the broader **Document Management System (DMS)**, it is designed to work **independently** and can be used in **any modern system** 
that requires secure identity management. 
It integrates with Keycloak for identity access and user management, and supports both user and machine-to-machine authentication.

---

## Features

🔒 User Authentication / Authorization ✅

🔑 Role-Based Access Control (RBAC) ✅

🔐 Multi-Factor Authentication (MFA) ✅

📧 Email-based Link Verification

📱 Sms based Verification

🌐 GSIS (www1.gsis.gr) Integration

👥 User Provisioning (Auto-Creating Users in Keycloak) ✅

🛡️ GDPR compliance through data anonymization.

🔗 Social Logins: (e.g. Google, Facebook, Apple ID)

📊 Admin Dashboard (optional UI)

---

## 🗃️ Database: PostgreSQL

This service uses **PostgreSQL** to persist data, such as: UserProfiles & TotpSecrets

---

## 📜 Logging - Serilog

This microservice uses **Serilog** for structured logging.
Serilog is configured to log to various sinks, including console, file, Seq, Elastic. 
The configuration can be found in the `appsettings.json` file.

---

## 🚀 Tech Stack

- .NET 9
- Keycloak
- PostgreSQL
- Otp.NET
- IMemoryCache
- Serilog for logging
- Clean Architecture (SOLID)

---

## MFA-First Login Flow with TOTP

This microservice handles **authentication and MFA (TOTP)** using:

- Keycloak (for token issuance and identity provider)
- TOTP (Time-based One-Time Password) as the MFA method
- Custom UI (not using Keycloak login screens)
- `IMemoryCache` for secure temporary state

### 🔐 TOTP Setup (One-time per user)

1. `POST /mfa/setup`  
   → Generates TOTP secret, QR code URI, and setup token  
   → Stores temporary secret in `IMemoryCache`

2. `POST /mfa/verify-setup`  
   → Validates 6-digit code  
   → If correct, stores TOTP secret to database  
   → Removes from cache


### 🔑 MFA Login Flow

1. `POST /auth/login`  
   → Validates username/password via Keycloak  
   → If MFA required:
     - Creates a `setup_token`
     - Stores `username`, `password`, `userId` in cache  
   → Returns `mfa_required = true` or token

2. `POST /mfa/verify-login`  
   → Validates 6-digit TOTP code  
   → If correct, issues Keycloak token using cached login  
   → Returns `access_token`, `refresh_token`

---

## Keycloak Configuration

### 1. Create Client
![Create Client](images/1.CreateClient.png)

### 2. Configure Client
![Configure Client](images/2.ConfigureClient.png)

### 3. Assign ServiceAccount Roles to Client
![Client Credentials](images/3.AssignServiceAccountRolesto Client.png)

### 4. Client-ClientScopes
![Client Scopes](images/4.Client-ClientScopes.png)

### 5. Configure a new Mapper
![Mappers](images/5.ConfigureNewMapper.png)

### 6. Add Mapper Audience
![Roles](images/6.AddMapperAudience.png)

### 7. Keep Client Secret
![Users](images/7.KeepClientSecret.png)

### 8. Realm Settings User Profile
![User Attributes](images/8.RealmSettingsUserProfile.png)

### 9. FirstName required Field Off
![FirstName required Field Off](images/9.FirstNameRequiredFieldOff.png)