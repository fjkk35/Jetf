package com.example.jetfapp.data.repository

import com.example.jetfapp.data.model.ApiResult
import com.example.jetfapp.data.model.AppConfig
import com.example.jetfapp.data.model.UnpackingBagNoRequest
import com.example.jetfapp.data.model.UnpackingBagResult
import com.example.jetfapp.data.model.UnpackingPendingRequest
import com.example.jetfapp.data.model.UnpackingTrackingNoRequest
import com.example.jetfapp.data.model.UnpackingTrackingResult
import com.example.jetfapp.network.PdtPortalApiService
import com.example.jetfapp.utils.HmacSigner
import com.google.gson.Gson
import java.time.Instant
import kotlinx.coroutines.CoroutineDispatcher
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

class UnpackingRepository(
    private val apiService: PdtPortalApiService,
    private val appConfig: AppConfig,
    private val gson: Gson,
    private val ioDispatcher: CoroutineDispatcher = Dispatchers.IO
) {
    suspend fun scanBagNo(request: UnpackingBagNoRequest): ApiResult<UnpackingBagResult> = withContext(ioDispatcher) {
        configFailure()?.let { return@withContext it }

        val timestamp = Instant.now().epochSecond
        val signature = HmacSigner.sign(timestamp = timestamp, request = request, secretKey = appConfig.hmacKey)

        safeApiCall(gson) {
            apiService.scanUnpackingBagNo(
                timestamp = timestamp,
                signature = signature,
                request = request
            )
        }
    }

    suspend fun scanTrackingNo(request: UnpackingTrackingNoRequest): ApiResult<UnpackingTrackingResult> = withContext(ioDispatcher) {
        configFailure()?.let { return@withContext it }

        val timestamp = Instant.now().epochSecond
        val signature = HmacSigner.sign(timestamp = timestamp, request = request, secretKey = appConfig.hmacKey)

        safeApiCall(gson) {
            apiService.scanUnpackingTrackingNo(
                timestamp = timestamp,
                signature = signature,
                request = request
            )
        }
    }

    suspend fun getPending(request: UnpackingPendingRequest): ApiResult<List<String>> = withContext(ioDispatcher) {
        if (!appConfig.hasBaseUrl) {
            ApiResult.Failure(message = "Missing API_BASE_URL configuration.")
        } else {
            safeApiCall(gson) {
                apiService.getUnpackingPending(request)
            }
        }
    }

    private fun configFailure(): ApiResult.Failure? {
        if (!appConfig.hasBaseUrl) {
            return ApiResult.Failure(message = "Missing API_BASE_URL configuration.")
        }
        if (!appConfig.hasHmacKey) {
            return ApiResult.Failure(
                message = "Missing PDT_HMAC_KEY configuration.",
                errorCode = "MISSING_HMAC_KEY"
            )
        }
        return null
    }
}
