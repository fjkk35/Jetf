package com.example.jetfapp.data.model

import com.google.gson.annotations.SerializedName

data class ApiEnvelope<T>(
    @SerializedName("isSuccess") val isSuccess: Boolean,
    @SerializedName("code") val code: Int,
    @SerializedName("message") val message: String,
    @SerializedName("errorCode") val errorCode: String,
    @SerializedName("timestamp") val timestamp: String,
    @SerializedName("data") val data: T
)

data class ApiErrorEnvelope(
    @SerializedName("message") val message: String? = null,
    @SerializedName("errorCode") val errorCode: String? = null,
    @SerializedName("code") val code: Int? = null
)

sealed class ApiResult<out T> {
    data class Success<T>(val data: T, val message: String) : ApiResult<T>()

    data class Failure(
        val message: String,
        val errorCode: String? = null,
        val code: Int? = null
    ) : ApiResult<Nothing>()
}

data class AppConfig(
    val baseUrl: String,
    val hmacKey: String,
    val scanIntentAction: String = "android.intent.action.SCANRESULT",
    val scanDataKeys: List<String> = listOf("SCAN_BARCODE_1", "barcode_string", "decode_data")
) {
    val hasBaseUrl: Boolean
        get() = baseUrl.isNotBlank()

    val hasHmacKey: Boolean
        get() = hmacKey.isNotBlank()
}

data class VersionCheckData(
    @SerializedName("latestVersionCode") val latestVersionCode: String,
    @SerializedName("apkUrl") val apkUrl: String,
    @SerializedName("forceUpdate") val forceUpdate: Boolean,
    @SerializedName("message") val message: String
)

data class SourceType(
    @SerializedName("id") val id: Int,
    @SerializedName("sourceType") val sourceType: String
)

data class LoginRequest(
    @SerializedName("account") val account: String,
    @SerializedName("versionCode") val versionCode: String
)

data class TrackingCheckRequest(
    @SerializedName("trackingNo") val trackingNo: String,
    @SerializedName("sourceType") val sourceType: Int
)

data class ShipmentInboundRequest(
    @SerializedName("inboundDate") val inboundDate: String,
    @SerializedName("trackingNo") val trackingNo: String,
    @SerializedName("seqNo") val seqNo: String,
    @SerializedName("locationCode") val locationCode: String,
    @SerializedName("sourceType") val sourceType: Int?,
    @SerializedName("returnTrackingNo") val returnTrackingNo: String,
    @SerializedName("size") val size: String,
    @SerializedName("uploadOpe") val uploadOpe: String
)
