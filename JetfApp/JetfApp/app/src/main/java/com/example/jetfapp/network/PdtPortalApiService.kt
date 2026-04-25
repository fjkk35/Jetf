package com.example.jetfapp.network

import com.example.jetfapp.data.model.ApiEnvelope
import com.example.jetfapp.data.model.BatchUpdateLocationCodeRequest
import com.example.jetfapp.data.model.GetBatchLocationUpdateCountRequest
import com.example.jetfapp.data.model.LoginRequest
import com.example.jetfapp.data.model.ShipmentInboundExceptionRequest
import com.example.jetfapp.data.model.ShipmentInboundRequest
import com.example.jetfapp.data.model.SourceType
import com.example.jetfapp.data.model.TrackingCheckRequest
import com.example.jetfapp.data.model.UpdateLocationCodeRequest
import com.example.jetfapp.data.model.VersionCheckData
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.Header
import retrofit2.http.POST
import retrofit2.http.Query

interface PdtPortalApiService {
    @GET("api/app/version-check")
    suspend fun checkVersion(
        @Query("versionCode") versionCode: String
    ): ApiEnvelope<VersionCheckData>

    @POST("api/auth/login")
    suspend fun login(
        @Body request: LoginRequest
    ): ApiEnvelope<Boolean>

    @GET("api/shipmentinbound/source-types")
    suspend fun getSourceTypes(): ApiEnvelope<List<SourceType>>

    @POST("api/shipmentinbound/check")
    suspend fun checkShipment(
        @Body request: TrackingCheckRequest
    ): ApiEnvelope<Boolean>

    @POST("api/shipmentinbound")
    suspend fun submitShipmentInbound(
        @Header("X-Timestamp") timestamp: Long,
        @Header("X-Signature") signature: String,
        @Body request: ShipmentInboundRequest
    ): ApiEnvelope<Boolean>

    @POST("api/shipmentinbound/exception")
    suspend fun submitShipmentInboundException(
        @Header("X-Timestamp") timestamp: Long,
        @Header("X-Signature") signature: String,
        @Body request: ShipmentInboundExceptionRequest
    ): ApiEnvelope<Boolean>

    @POST("api/shipmentinbound/update-location-code")
    suspend fun updateLocationCode(
        @Header("X-Timestamp") timestamp: Long,
        @Header("X-Signature") signature: String,
        @Body request: UpdateLocationCodeRequest
    ): ApiEnvelope<Boolean>

    @POST("api/shipmentinbound/batch-location-update-count")
    suspend fun getBatchLocationUpdateCount(
        @Header("X-Timestamp") timestamp: Long,
        @Header("X-Signature") signature: String,
        @Body request: GetBatchLocationUpdateCountRequest
    ): ApiEnvelope<Boolean>

    @POST("api/shipmentinbound/batch-update-location-code")
    suspend fun batchUpdateLocationCode(
        @Header("X-Timestamp") timestamp: Long,
        @Header("X-Signature") signature: String,
        @Body request: BatchUpdateLocationCodeRequest
    ): ApiEnvelope<Boolean>
}
