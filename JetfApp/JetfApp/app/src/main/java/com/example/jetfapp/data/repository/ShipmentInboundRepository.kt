package com.example.jetfapp.data.repository

import com.example.jetfapp.data.model.ApiResult
import com.example.jetfapp.data.model.AppConfig
import com.example.jetfapp.data.model.ShipmentInboundExceptionRequest
import com.example.jetfapp.data.model.ShipmentInboundRequest
import com.example.jetfapp.data.model.SourceType
import com.example.jetfapp.data.model.TrackingCheckRequest
import com.example.jetfapp.network.PdtPortalApiService
import com.example.jetfapp.utils.HmacSigner
import com.google.gson.Gson
import java.time.Instant
import kotlinx.coroutines.CoroutineDispatcher
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

class ShipmentInboundRepository(
    private val apiService: PdtPortalApiService,
    private val appConfig: AppConfig,
    private val gson: Gson,
    private val ioDispatcher: CoroutineDispatcher = Dispatchers.IO
) {
    suspend fun getSourceTypes(): ApiResult<List<SourceType>> = withContext(ioDispatcher) {
        if (!appConfig.hasBaseUrl) {
            ApiResult.Failure(message = "Missing API_BASE_URL configuration.")
        } else {
            safeApiCall(gson) {
                apiService.getSourceTypes()
            }
        }
    }

    suspend fun checkShipment(trackingNo: String, sourceType: Int): ApiResult<Boolean> = withContext(ioDispatcher) {
        if (!appConfig.hasBaseUrl) {
            ApiResult.Failure(message = "Missing API_BASE_URL configuration.")
        } else {
            safeApiCall(gson) {
                apiService.checkShipment(
                    TrackingCheckRequest(
                        trackingNo = trackingNo,
                        sourceType = sourceType
                    )
                )
            }
        }
    }

    suspend fun submitInbound(request: ShipmentInboundRequest): ApiResult<Boolean> = withContext(ioDispatcher) {
        if (!appConfig.hasBaseUrl) {
            return@withContext ApiResult.Failure(message = "Missing API_BASE_URL configuration.")
        }
        if (!appConfig.hasHmacKey) {
            return@withContext ApiResult.Failure(
                message = "Missing PDT_HMAC_KEY configuration.",
                errorCode = "MISSING_HMAC_KEY"
            )
        }

        val timestamp = Instant.now().epochSecond
        val signature = HmacSigner.sign(timestamp = timestamp, request = request, secretKey = appConfig.hmacKey)

        safeApiCall(gson) {
            apiService.submitShipmentInbound(
                timestamp = timestamp,
                signature = signature,
                request = request
            )
        }
    }

    suspend fun submitInboundException(request: ShipmentInboundExceptionRequest): ApiResult<Boolean> = withContext(ioDispatcher) {
        if (!appConfig.hasBaseUrl) {
            return@withContext ApiResult.Failure(message = "Missing API_BASE_URL configuration.")
        }
        if (!appConfig.hasHmacKey) {
            return@withContext ApiResult.Failure(
                message = "Missing PDT_HMAC_KEY configuration.",
                errorCode = "MISSING_HMAC_KEY"
            )
        }

        val timestamp = Instant.now().epochSecond
        val signature = HmacSigner.sign(timestamp = timestamp, request = request, secretKey = appConfig.hmacKey)

        safeApiCall(gson) {
            apiService.submitShipmentInboundException(
                timestamp = timestamp,
                signature = signature,
                request = request
            )
        }
    }
}
