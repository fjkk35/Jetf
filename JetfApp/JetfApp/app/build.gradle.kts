import java.util.Properties

plugins {
    alias(libs.plugins.android.application)
    alias(libs.plugins.kotlin.android)
}

val localProperties = Properties().apply {
    val localPropertiesFile = rootProject.file("local.properties")
    if (localPropertiesFile.exists()) {
        localPropertiesFile.inputStream().use(::load)
    }
}

fun Project.propertyOrDefault(name: String, defaultValue: String): String {
    return (findProperty(name) as String?) ?: localProperties.getProperty(name, defaultValue)
}

fun escapeForBuildConfig(value: String): String {
    return value.replace("\\", "\\\\").replace("\"", "\\\"")
}

fun versionCodeFromVersionName(versionName: String): Int {
    val parts = versionName.split('.')
        .map { it.toIntOrNull() ?: 0 }
        .take(3)
        .toMutableList()

    while (parts.size < 3) {
        parts += 0
    }

    return (parts[0] * 10_000) + (parts[1] * 100) + parts[2]
}

val apiBaseUrl = project.propertyOrDefault("API_BASE_URL", "http://localhost:5260/")
val normalizedApiBaseUrl = if (apiBaseUrl.endsWith('/')) apiBaseUrl else "$apiBaseUrl/"
val releaseApiBaseUrl = "https://service.jet-f.com/PdtPortalAPI/"
val hmacKey = project.propertyOrDefault("PDT_HMAC_KEY", "")
val appVersionName = "0.0.4"

android {
    namespace = "com.example.jetfapp"
    compileSdk {
        version = release(36)
    }

    defaultConfig {
        applicationId = "com.example.jetfapp"
        minSdk = 28
        targetSdk = 36
        versionCode = versionCodeFromVersionName(appVersionName)
        versionName = appVersionName

        buildConfigField("String", "PDT_HMAC_KEY", "\"${escapeForBuildConfig(hmacKey)}\"")

        testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"
    }

    buildTypes {
        debug {
            buildConfigField("String", "API_BASE_URL", "\"${escapeForBuildConfig(normalizedApiBaseUrl)}\"")
        }

        release {
            isMinifyEnabled = false
            buildConfigField("String", "API_BASE_URL", "\"${escapeForBuildConfig(releaseApiBaseUrl)}\"")
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro"
            )
        }
    }
    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_11
        targetCompatibility = JavaVersion.VERSION_11
    }
    kotlinOptions {
        jvmTarget = "11"
    }
    buildFeatures {
        buildConfig = true
        viewBinding = true
    }
}

dependencies {
    implementation(libs.androidx.core.ktx)
    implementation(libs.androidx.appcompat)
    implementation(libs.material)
    implementation(libs.androidx.activity)
    implementation(libs.androidx.constraintlayout)
    implementation(libs.androidx.fragment.ktx)
    implementation(libs.androidx.lifecycle.runtime.ktx)
    implementation(libs.androidx.lifecycle.viewmodel.ktx)
    implementation(libs.androidx.lifecycle.livedata.ktx)
    implementation(libs.kotlinx.coroutines.android)
    implementation(libs.retrofit)
    implementation(libs.retrofit.converter.gson)
    implementation(libs.okhttp)
    implementation(libs.okhttp.logging)
    implementation(libs.gson)
    testImplementation(libs.junit)
    androidTestImplementation(libs.androidx.junit)
    androidTestImplementation(libs.androidx.espresso.core)
}