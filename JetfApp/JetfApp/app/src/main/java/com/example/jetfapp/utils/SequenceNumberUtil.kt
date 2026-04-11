package com.example.jetfapp.utils

import java.util.Locale

object SequenceNumberUtil {
    private val sequencePattern = Regex("^[A-Z]{2}[0-9]{4}$")

    fun normalize(rawValue: String): String {
        return rawValue.trim().uppercase(Locale.US)
    }

    fun isValid(rawValue: String): Boolean {
        return sequencePattern.matches(normalize(rawValue))
    }

    fun nextOrNull(currentValue: String): String? {
        val normalized = normalize(currentValue)
        if (!isValid(normalized)) {
            return null
        }

        val prefix = normalized.take(2)
        val numericPart = normalized.takeLast(4).toInt()
        if (numericPart >= 9999) {
            return null
        }

        return prefix + (numericPart + 1).toString().padStart(4, '0')
    }
}