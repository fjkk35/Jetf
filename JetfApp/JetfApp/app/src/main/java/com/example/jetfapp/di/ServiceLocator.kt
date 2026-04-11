package com.example.jetfapp.di

import com.example.jetfapp.BuildConfig
import com.example.jetfapp.data.model.AppConfig
import com.example.jetfapp.data.repository.AppRepository
import com.example.jetfapp.data.repository.ShipmentInboundRepository
import com.example.jetfapp.network.PdtPortalApiService
import com.google.gson.Gson
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory

object ServiceLocator {
    private val gson: Gson by lazy { Gson() }

    private val appConfig: AppConfig by lazy {
        AppConfig(
            baseUrl = BuildConfig.API_BASE_URL.trim(),
            hmacKey = BuildConfig.PDT_HMAC_KEY.trim()
        )
    }

    private val apiService: PdtPortalApiService by lazy {
        val logging = HttpLoggingInterceptor().apply {
            level = HttpLoggingInterceptor.Level.BASIC
        }

        val client = OkHttpClient.Builder()
            .addInterceptor(logging)
            .build()

        Retrofit.Builder()
            .baseUrl(appConfig.baseUrl)
            .client(client)
            .addConverterFactory(GsonConverterFactory.create(gson))
            .build()
            .create(PdtPortalApiService::class.java)
    }

    private val appRepository: AppRepository by lazy {
        AppRepository(apiService = apiService, appConfig = appConfig, gson = gson)
    }

    private val shipmentInboundRepository: ShipmentInboundRepository by lazy {
        ShipmentInboundRepository(apiService = apiService, appConfig = appConfig, gson = gson)
    }

    fun provideAppConfig(): AppConfig = appConfig

    fun provideAppRepository(): AppRepository = appRepository

    fun provideShipmentInboundRepository(): ShipmentInboundRepository = shipmentInboundRepository
}