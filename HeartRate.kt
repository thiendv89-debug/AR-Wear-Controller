package com.stem.arcontroller.presentation

import android.content.Context
import android.util.Log
import androidx.health.services.client.HealthServices
import androidx.health.services.client.MeasureCallback
import androidx.health.services.client.data.Availability
import androidx.health.services.client.data.DataPointContainer
import androidx.health.services.client.data.DataType
import androidx.health.services.client.data.DataTypeAvailability
import androidx.health.services.client.data.DeltaDataType

class HeartRateReader(
    context: Context,
    private val onBpm: (Int) -> Unit,
    private val onStatus: (String) -> Unit
) {
    private val measureClient = HealthServices.getClient(context).measureClient
    private var started = false

    private val callback = object : MeasureCallback {
        override fun onAvailabilityChanged(
            dataType: DeltaDataType<*, *>,
            availability: Availability
        ) {
            if (availability is DataTypeAvailability) onStatus(availability.toString())
        }

        override fun onDataReceived(data: DataPointContainer) {
            val sample = data.getData(DataType.HEART_RATE_BPM).lastOrNull() ?: return
            onBpm(sample.value.toInt())
        }
    }

    fun start() {
        if (started) return
        started = true
        try {
            measureClient.registerMeasureCallback(DataType.HEART_RATE_BPM, callback)
        } catch (e: Exception) {
            started = false
            Log.e("HeartRate", "register failed", e)
            onStatus("ERROR")
        }
    }
}
// END OF FILE