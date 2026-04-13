package com.example.jetfapp.utils

import com.example.jetfapp.data.model.ShipmentInboundRequest
import java.time.OffsetDateTime
import java.time.format.DateTimeFormatter
import javax.crypto.Mac
import javax.crypto.spec.SecretKeySpec

object HmacSigner {
    private val canonicalInboundDateFormatter: DateTimeFormatter =
        DateTimeFormatter.ofPattern("uuuu-MM-dd'T'HH:mm:ss.SSSSSSSXXX")

    fun sign(timestamp: Long, request: ShipmentInboundRequest, secretKey: String): String {
        val canonicalInboundDate = OffsetDateTime.parse(request.inboundDate).format(canonicalInboundDateFormatter)
        val payload = buildString {
            appendLine(timestamp)
            appendLine(canonicalInboundDate)
            appendLine(request.trackingNo)
            appendLine(request.seqNo)
            appendLine(request.locationCode)
            appendLine(request.sourceType?.toString().orEmpty())
            appendLine(request.returnTrackingNo)
            append(request.uploadOpe)
        }

        val mac = Mac.getInstance("HmacSHA256")
        mac.init(SecretKeySpec(secretKey.toByteArray(Charsets.UTF_8), "HmacSHA256"))
        return mac.doFinal(payload.toByteArray(Charsets.UTF_8)).joinToString(separator = "") { byte ->
            "%02x".format(byte)
        }
    }
}