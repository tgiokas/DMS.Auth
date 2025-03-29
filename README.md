# 🔐 DMS.Auth — MFA-First Login Flow with TOTP and Keycloak

This microservice handles **authentication and MFA (TOTP)** for users of the DMS system, using:

- ✅ Keycloak (for token issuance and identity provider)
- ✅ TOTP (Time-based One-Time Password) as the MFA method
- ✅ Custom UI (not using Keycloak login screens)
- ✅ `IMemoryCache` for secure temporary state
- ✅ Clean Architecture with SOLID principles

---

## 🧭 Flow Overview

### 🔐 TOTP Setup (One-time per user)
1. `POST /mfa/setup`  
   → Generates TOTP secret, QR code URI, and setup token  
   → Stores temporary secret in `IMemoryCache`

2. `POST /mfa/verify-setup`  
   → Validates 6-digit code  
   → If correct, stores TOTP secret to database  
   → Removes from cache

---

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

## 🔐 Security Notes

- ✅ Passwords are stored **temporarily** in-memory (not persisted)
- ✅ Token is only issued **after MFA verification passes**
- ✅ All secrets and attempts auto-expire in 5 minutes
- ❌ No sensitive data is logged or serialized

---

## 🚀 Tech Stack

- .NET 8
- Keycloak
- Otp.NET
- IMemoryCache
- Clean Architecture


