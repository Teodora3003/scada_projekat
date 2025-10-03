using Common;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ProcessingModule
{
    /// <summary>
    /// Class containing logic for automated work.
    /// </summary>
    public class AutomationManager : IAutomationManager, IDisposable
	{
		private Thread automationWorker;
        private AutoResetEvent automationTrigger;
        private IStorage storage;
		private IProcessingManager processingManager;
		private int delayBetweenCommands;
        private IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomationManager"/> class.
        /// </summary>
        /// <param name="storage">The storage.</param>
        /// <param name="processingManager">The processing manager.</param>
        /// <param name="automationTrigger">The automation trigger.</param>
        /// <param name="configuration">The configuration.</param>
        public AutomationManager(IStorage storage, IProcessingManager processingManager, AutoResetEvent automationTrigger, IConfiguration configuration)
		{
			this.storage = storage;
			this.processingManager = processingManager;
            this.configuration = configuration;
            this.automationTrigger = automationTrigger;
        }

        /// <summary>
        /// Initializes and starts the threads.
        /// </summary>
		private void InitializeAndStartThreads()
		{
			InitializeAutomationWorkerThread();
			StartAutomationWorkerThread();
		}

        /// <summary>
        /// Initializes the automation worker thread.
        /// </summary>
		private void InitializeAutomationWorkerThread()
		{
			automationWorker = new Thread(AutomationWorker_DoWork);
			automationWorker.Name = "Aumation Thread";
		}

        /// <summary>
        /// Starts the automation worker thread.
        /// </summary>
		private void StartAutomationWorkerThread()
		{
			automationWorker.Start();
		}


        private void AutomationWorker_DoWork()
        {
            {
                while (!disposedValue)
                {
                    // Čekaj na signal (svake sekunde)
                    automationTrigger.WaitOne();

                    try
                    {
                        // ========== SIMULACIJA PUNJENJA/PRAŽNJENJA ==========
                        SimulateBatteryLevel();

                        // Delay između komandi da simulator stigne da obradi
                        Thread.Sleep(delayBetweenCommands);

                        // Pronađi kapacitet baterije K (adresa 2000)
                        List<IPoint> batteryPoints = storage.GetPoints(
                            new List<PointIdentifier> { new PointIdentifier(PointType.ANALOG_OUTPUT, 2000) }
                        );

                        if (batteryPoints.Count > 0)
                        {
                            IAnalogPoint battery = batteryPoints[0] as IAnalogPoint;

                            // ========== PROVERA ZA LOW ALARM ==========
                            if (battery.EguValue < battery.ConfigItem.LowLimit)
                            {
                                // ... (sve kao pre)
                                battery.Alarm = AlarmType.LOW_ALARM;

                                // ISKLJUČI T4
                                List<IPoint> t4Points = storage.GetPoints(
                                    new List<PointIdentifier> { new PointIdentifier(PointType.DIGITAL_OUTPUT, 1003) }
                                );
                                if (t4Points.Count > 0)
                                {
                                    IDigitalPoint t4 = t4Points[0] as IDigitalPoint;
                                    if (t4.State == DState.ON)
                                    {
                                        processingManager.ExecuteWriteCommand(
                                            t4.ConfigItem,
                                            configuration.GetTransactionId(),
                                            configuration.UnitAddress,
                                            1003, 0
                                        );
                                        Thread.Sleep(delayBetweenCommands);
                                    }
                                }

                                // UKLJUČI I2
                                List<IPoint> i2Points = storage.GetPoints(
                                    new List<PointIdentifier> { new PointIdentifier(PointType.DIGITAL_OUTPUT, 3001) }
                                );
                                if (i2Points.Count > 0)
                                {
                                    IDigitalPoint i2 = i2Points[0] as IDigitalPoint;
                                    if (i2.State == DState.OFF)
                                    {
                                        processingManager.ExecuteWriteCommand(
                                            i2.ConfigItem,
                                            configuration.GetTransactionId(),
                                            configuration.UnitAddress,
                                            3001, 1
                                        );
                                        Thread.Sleep(delayBetweenCommands);
                                    }
                                }
                            }
                            // ========== PROVERA ZA EGU MAX ==========
                            else if (battery.EguValue >= battery.ConfigItem.EGU_Max)
                            {
                                // ISKLJUČI I1
                                List<IPoint> i1Points = storage.GetPoints(
                                    new List<PointIdentifier> { new PointIdentifier(PointType.DIGITAL_OUTPUT, 3000) }
                                );
                                if (i1Points.Count > 0)
                                {
                                    IDigitalPoint i1 = i1Points[0] as IDigitalPoint;
                                    if (i1.State == DState.ON)
                                    {
                                        processingManager.ExecuteWriteCommand(
                                            i1.ConfigItem,
                                            configuration.GetTransactionId(),
                                            configuration.UnitAddress,
                                            3000, 0
                                        );
                                        Thread.Sleep(delayBetweenCommands);
                                    }
                                }

                                // ISKLJUČI I2
                                List<IPoint> i2Points = storage.GetPoints(
                                    new List<PointIdentifier> { new PointIdentifier(PointType.DIGITAL_OUTPUT, 3001) }
                                );
                                if (i2Points.Count > 0)
                                {
                                    IDigitalPoint i2 = i2Points[0] as IDigitalPoint;
                                    if (i2.State == DState.ON)
                                    {
                                        processingManager.ExecuteWriteCommand(
                                            i2.ConfigItem,
                                            configuration.GetTransactionId(),
                                            configuration.UnitAddress,
                                            3001, 0
                                        );
                                        Thread.Sleep(delayBetweenCommands);
                                    }
                                }

                                battery.Alarm = AlarmType.NO_ALARM;
                            }
                            else
                            {
                                battery.Alarm = AlarmType.NO_ALARM;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log greške
                    }
                }
            }
        }

        /// <summary>
        /// Simulates battery charging/discharging based on connected devices.
        /// </summary>
        private void SimulateBatteryLevel()
        {
            try
            {
                // Pronađi kapacitet baterije K (adresa 2000)
                List<IPoint> batteryPoints = storage.GetPoints(
                    new List<PointIdentifier> { new PointIdentifier(PointType.ANALOG_OUTPUT, 2000) }
                );

                if (batteryPoints.Count == 0)
                    return;

                IAnalogPoint battery = batteryPoints[0] as IAnalogPoint;
                double currentCapacity = battery.EguValue;
                double capacityChange = 0;

                // Proveri T1 (USB1, adresa 1000) - troši 1%
                List<IPoint> t1Points = storage.GetPoints(
                    new List<PointIdentifier> { new PointIdentifier(PointType.DIGITAL_OUTPUT, 1000) }
                );
                if (t1Points.Count > 0 && (t1Points[0] as IDigitalPoint).State == DState.ON)
                {
                    capacityChange -= 1;
                }

                // Proveri T2 (USB2, adresa 1001) - troši 1%
                List<IPoint> t2Points = storage.GetPoints(
                    new List<PointIdentifier> { new PointIdentifier(PointType.DIGITAL_OUTPUT, 1001) }
                );
                if (t2Points.Count > 0 && (t2Points[0] as IDigitalPoint).State == DState.ON)
                {
                    capacityChange -= 1;
                }

                // Proveri T3 (USB3, adresa 1002) - troši 1%
                List<IPoint> t3Points = storage.GetPoints(
                    new List<PointIdentifier> { new PointIdentifier(PointType.DIGITAL_OUTPUT, 1002) }
                );
                if (t3Points.Count > 0 && (t3Points[0] as IDigitalPoint).State == DState.ON)
                {
                    capacityChange -= 1;
                }

                // Proveri T4 (utičnica, adresa 1003) - troši 3%
                List<IPoint> t4Points = storage.GetPoints(
                    new List<PointIdentifier> { new PointIdentifier(PointType.DIGITAL_OUTPUT, 1003) }
                );
                if (t4Points.Count > 0 && (t4Points[0] as IDigitalPoint).State == DState.ON)
                {
                    capacityChange -= 3;
                }

                // Proveri I1 (napajanje 1, adresa 3000) - puni 2%
                List<IPoint> i1Points = storage.GetPoints(
                    new List<PointIdentifier> { new PointIdentifier(PointType.DIGITAL_OUTPUT, 3000) }
                );
                if (i1Points.Count > 0 && (i1Points[0] as IDigitalPoint).State == DState.ON)
                {
                    capacityChange += 2;
                }

                // Proveri I2 (napajanje 2, adresa 3001) - puni 3%
                List<IPoint> i2Points = storage.GetPoints(
                    new List<PointIdentifier> { new PointIdentifier(PointType.DIGITAL_OUTPUT, 3001) }
                );
                if (i2Points.Count > 0 && (i2Points[0] as IDigitalPoint).State == DState.ON)
                {
                    capacityChange += 3;
                }

                // Ažuriraj kapacitet
                double newCapacity = currentCapacity + capacityChange;

                // Ograniči na 0-100
                if (newCapacity < 0)
                    newCapacity = 0;
                if (newCapacity > battery.ConfigItem.EGU_Max)
                    newCapacity = battery.ConfigItem.EGU_Max;

                // Pošalji novu vrednost samo ako se promenila
                if (Math.Abs(newCapacity - currentCapacity) > 0.01)
                {
                    // Konvertuj u raw vrednost i pošalji
                    processingManager.ExecuteWriteCommand(
                        battery.ConfigItem,
                        configuration.GetTransactionId(),
                        configuration.UnitAddress,
                        2000, // adresa K
                        (int)newCapacity
                    );
                }
            }
            catch (Exception ex)
            {
                // Log greške (opciono)
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls


        /// <summary>
        /// Disposes the object.
        /// </summary>
        /// <param name="disposing">Indication if managed objects should be disposed.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
				}
				disposedValue = true;
			}
		}


		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// GC.SuppressFinalize(this);
		}

        /// <inheritdoc />
        public void Start(int delayBetweenCommands)
		{
			this.delayBetweenCommands = delayBetweenCommands*1000;
            InitializeAndStartThreads();
		}

        /// <inheritdoc />
        public void Stop()
		{
			Dispose();
		}
		#endregion
	}
}
