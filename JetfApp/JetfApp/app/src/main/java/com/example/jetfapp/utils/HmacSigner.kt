package com.example.jetfapp.utils

import com.example.jetfapp.data.model.ShipmentInboundRequest
import javax.crypto.Mac
import javax.crypto.spec.SecretKeySpec

object HmacSigner {
    fun sign(timestamp: Long, request: ShipmentInboundRequest, secretKey: String): String {
        val payload = buildString {
            appendLine(timestamp)
            appendLine(request.inboundDate)
            appendLine(request.trackingNo)
            appendLine(request.seqNo)
            appendLine(request.locationCode)
            appendLine(request.sourceType?.toString().orEmpty())
            append(request.returnTrackingNo)
        }

        val mac = Mac.getInstance("HmacSHA256")
        mac.init(SecretKeySpec(secretKey.toByteArray(Charsets.UTF_8), "HmacSHA256"))
        return mac.doFinal(payload.toByteArray(Charsets.UTF_8)).joinToString(separator = "") { byte ->
            "%02x".format(byte)
        }
    }
}