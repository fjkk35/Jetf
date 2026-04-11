package com.example.jetfapp.data.repository

import com.example.jetfapp.data.model.ApiEnvelope
import com.example.jetfapp.data.model.ApiErrorEnvelope
import com.example.jetfapp.data.model.ApiResult
import com.google.gson.Gson
import java.io.IOException
import retrofit2.HttpException

internal suspend fun <T> safeApiCall(
    gson: Gson,
    apiCall: suspend () -> ApiEnvelope<T>
): ApiResult<T> {
    return try {
        val response = apiCall()
        if (response.isSuccess) {
            ApiResult.Success(response.data, response.message)
        } else {
            ApiResult.Failure(
                message = response.message.ifBlank { "作業失敗" },
                errorCode = response.errorCode,
                code = response.code
            )
        }
    } catch (exception: HttpException) {
        parseHttpFailure(gson, exception)
    } catch (_: IOException) {
        ApiResult.Failure(message = "無法連線到伺服器，請確認網路與 API 設定。")
    } catch (exception: Exception) {
        ApiResult.Failure(message = exception.message ?: "發生未預期錯誤。")
    }
}

private fun parseHttpFailure(gson: Gson, exception: HttpException): ApiResult.Failure {
    val errorBody = exception.response()?.errorBody()?.string().orEmpty()
    val parsed = runCatching {
        gson.fromJson(errorBody, ApiErrorEnvelope::class.java)
    }.getOrNull()

    return ApiResult.Failure(
        message = parsed?.message?.ifBlank { null }
            ?: exception.message()
            ?: "伺服器回應錯誤。",
        errorCode = parsed?.errorCode,
        code = parsed?.code ?: exception.code()
    )
}