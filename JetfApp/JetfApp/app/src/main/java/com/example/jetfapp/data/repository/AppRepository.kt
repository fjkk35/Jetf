package com.example.jetfapp.data.repository

import com.example.jetfapp.data.model.ApiResult
import com.example.jetfapp.data.model.AppConfig
import com.example.jetfapp.data.model.LoginRequest
import com.example.jetfapp.data.model.VersionCheckData
import com.example.jetfapp.network.PdtPortalApiService
import com.google.gson.Gson
import kotlinx.coroutines.CoroutineDispatcher
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

class AppRepository(
    private val apiService: PdtPortalApiService,
    private val appConfig: AppConfig,
    private val gson: Gson,
    private val ioDispatcher: CoroutineDispatcher = Dispatchers.IO
) {
    suspend fun checkVersion(versionCode: String): ApiResult<VersionCheckData> = withContext(ioDispatcher) {
        if (!appConfig.hasBaseUrl) {
            ApiResult.Failure(message = "Missing API_BASE_URL configuration.")
        } else {
            safeApiCall(gson) {
                apiService.checkVersion(versionCode)
            }
        }
    }

    suspend fun login(account: String, versionCode: String): ApiResult<Boolean> = withContext(ioDispatcher) {
        if (!appConfig.hasBaseUrl) {
            ApiResult.Failure(message = "Missing API_BASE_URL configuration.")
        } else {
            safeApiCall(gson) {
                apiService.login(LoginRequest(account = account, versionCode = versionCode))
            }
        }
    }
}