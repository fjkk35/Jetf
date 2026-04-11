package com.example.jetfapp.utils

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

class SequenceNumberUtilTest {
    @Test
    fun `normalize converts lowercase input to uppercase`() {
        assertEquals("AB0001", SequenceNumberUtil.normalize("ab0001"))
    }

    @Test
    fun `isValid accepts two letters and four digits`() {
        assertTrue(SequenceNumberUtil.isValid("AB0001"))
        assertFalse(SequenceNumberUtil.isValid("A10001"))
        assertFalse(SequenceNumberUtil.isValid("ABC001"))
    }

    @Test
    fun `nextOrNull increments sequence within bounds`() {
        assertEquals("AB0002", SequenceNumberUtil.nextOrNull("AB0001"))
    }

    @Test
    fun `nextOrNull returns null at upper bound`() {
        assertNull(SequenceNumberUtil.nextOrNull("AB9999"))
    }
}