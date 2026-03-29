# Copilot Instructions for Android DT40 Project (Android 9 Compatible)

## 📱 Project Overview

This project is an Android application designed specifically for **Urovo DT40 industrial devices**.

* Platform: Android (Enterprise device)
* Device: Urovo DT40 (built-in barcode scanner)
* OS Version: **Android 9 (API 28)**
* Usage: Internal enterprise app (not for Google Play)
* Priority: Stability > Compatibility

---

## ⚙️ Technical Stack

* Language: **Kotlin**
* Minimum SDK: **API 28 (Android 9)** ✅
* Target SDK: **Latest stable (API 34+)**
* Compile SDK: **Latest stable**
* Architecture: **MVVM**
* UI: **XML (View-based)** (DO NOT use Jetpack Compose unless explicitly requested)
* Networking: **Retrofit + OkHttp**
* JSON: **Gson or Moshi**
* Async: **Kotlin Coroutines + Flow**
* Binding: **ViewBinding**

---

## ⚠️ Android Version Compatibility Rules (IMPORTANT)

* Base development MUST be compatible with **Android 9 (API 28)**
* DO NOT use APIs that require Android 10+ unless guarded

### Required Pattern:

```kotlin
if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
    // Android 10+ API
} else {
    // Android 9 compatible implementation
}
```

* Avoid Android 10 scoped storage-only APIs unless necessary
* Prefer backward-compatible implementations

---

## 🧱 Project Structure

```
com.company.app
│
├── ui/                # Activities / Fragments
├── viewmodel/         # ViewModels
├── repository/        # Data logic
├── data/
│   ├── remote/        # API services
│   └── local/         # Local DB / storage
├── scanner/           # Urovo scanner integration
├── utils/             # Extensions / helpers
├── base/              # Base classes
└── model/             # Data models
```

---

## 📦 Coding Guidelines

### General Rules

* Use **idiomatic Kotlin**
* Prefer `val` over `var`
* Keep functions short and readable
* Avoid unnecessary complexity

---

### Naming Conventions

* Class: `PascalCase`
* Function/Variable: `camelCase`
* Constant: `UPPER_SNAKE_CASE`
* XML ID: `snake_case`

---

## 🏗️ Architecture Rules (MVVM)

* Activity/Fragment → UI only
* ViewModel → business logic
* Repository → data handling
* DO NOT call API directly in Activity
* DO NOT put logic inside XML

---

## 📡 Networking Rules

* Use Retrofit interfaces
* Always handle:

  * Success
  * Error
  * Exception

Use unified result wrapper:

```kotlin
sealed class Result<out T> {
    data class Success<T>(val data: T) : Result<T>()
    data class Error(val exception: Exception) : Result<Nothing>()
}
```

---

## 🔍 DT40 Barcode Scanner Rules

* Use **Urovo ScanManager / official SDK**

* DO NOT use camera-based scanning

* Scanner lifecycle:

  * Initialize in `onResume`
  * Release in `onPause`

* Handle scan result via:

  * BroadcastReceiver OR SDK callback

* Must support:

  * Physical trigger button
  * Programmatic trigger (startDecode / stopDecode)

* Always consider:

  * Rapid continuous scanning
  * Debounce handling

---

## 🔐 Permissions (Android 9 Focus)

* Use runtime permissions when required
* DO NOT rely on Android 10 scoped storage only
* Prefer legacy-compatible file access

---

## 🧪 Logging

* Use `Log.d()` for debug
* Tag = class name
* Avoid excessive logs

---

## 🚫 Strictly Avoid

* AsyncTask
* Blocking main thread
* Deprecated APIs
* Over-engineered patterns
* Jetpack Compose (unless requested)
* Android 10+ only APIs without version check

---

## ✅ Preferred Practices

* Use ViewBinding
* Use Coroutines (no Thread / Handler)
* Use `when` instead of complex if-else
* Use extension functions

---

## 🌏 Language Preferences

* Code must be written in **English**
* Class names, variables, functions → **English only**
* Comments → **Traditional Chinese**
* Explanations → **Traditional Chinese**
* Keep technical terms in English (ViewModel, API, Repository)

---

## 🧠 Copilot Behavior Rules

When generating code:

* Always generate **complete classes**, not fragments
* Include necessary imports
* Follow MVVM structure strictly
* Add basic error handling
* DO NOT generate Android 10-only implementations
* Do NOT guess unknown SDKs (especially scanner APIs)
* If unsure about DT40 SDK → leave TODO comment instead of guessing

---

## 📝 Comment Style (IMPORTANT)

All comments must be written in Traditional Chinese.

Example:

```kotlin
// 初始化掃碼器（DT40 專用）
private fun initScanner() {
    // TODO: 使用 Urovo SDK 初始化
}
```

---

## 📄 Output Expectations

* Clean, production-ready code
* Minimal but meaningful comments
* No placeholder pseudo-code
* Avoid unnecessary explanations inside code

---

## 🚀 Special Notes

* This app runs on **single device model (DT40 Android 9)**
* No need to support legacy Android versions below API 28
* Scanner feature is **core functionality**
* Stability is more important than flexibility

---

## 🔐 Authentication (JWT - .NET API)

* Backend API is built with **.NET (ASP.NET Web API)**
* Authentication method: **JWT (JSON Web Token)**

### Requirements

* All authenticated requests must include:

```
Authorization: Bearer <token>
```

* Token is obtained from login API
* Token must be stored securely (SharedPreferences or DataStore)

---

### Login Flow

1. Call login API with credentials
2. Receive JWT token
3. Store token locally
4. Attach token to all subsequent API requests

---

### Networking Rules for JWT

* Use **OkHttp Interceptor** to automatically attach token
* Do NOT manually add token in every API call
* Handle token expiration:

  * Return to login screen OR refresh token if API supports it

---

### Security Rules

* Do NOT hardcode tokens
* Do NOT expose sensitive data in logs
* Always handle unauthorized (401) responses

---

### Copilot Instructions

When generating API-related code:

* Always include JWT handling
* Always implement interceptor for Authorization header
* Assume backend is **.NET API**
* Use proper error handling for authentication failures

---
