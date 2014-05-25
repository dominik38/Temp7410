using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

namespace Gadgeteer.Modules.SchreiberDominik
{
    public static class I2CTransactions
    {
        const int DefaultTimeout = 1000;

        /// <summary>
        /// Executes the passed transactions.
        /// </summary>
        /// <param name="transactions"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static int Execute(I2CDevice device, I2CDevice.I2CTransaction[] transactions, int timeout)
        {
            return device.Execute(transactions, timeout);
        }

        /// <summary>
        /// Simple read of device.
        /// </summary>
        /// <param name="memoryAddress">Initial register address.</param>
        /// <param name="responseLength">Optional read length.</param>
        /// <returns></returns>
        public static byte[] Read(I2CDevice device, byte memoryAddress, int responseLength = 1, int timeout = DefaultTimeout)
        {
            var buffer = new byte[responseLength];
            I2CDevice.I2CTransaction[] transaction;
            transaction = new I2CDevice.I2CTransaction[]
            {
                I2CDevice.CreateWriteTransaction(new byte[] { memoryAddress}),
                I2CDevice.CreateReadTransaction(buffer)
            };
            int result = Execute(device, transaction, timeout);
            return buffer;
        }

        /// <summary>
        /// Simple read of device. However no address is supplied, so the device must be at the correct address, or not need one.
        /// </summary>
        /// <param name="responseLength">Optional read length.</param>
        /// <returns></returns>
        public static byte[] Read(I2CDevice device, int responseLength = 1, int timeout = DefaultTimeout)
        {
            var buffer = new byte[responseLength];
            I2CDevice.I2CTransaction[] transaction;
            transaction = new I2CDevice.I2CTransaction[]
            {
                I2CDevice.CreateReadTransaction(buffer)
            };
            int result = Execute(device, transaction, timeout);
            return buffer;
        }

        /// <summary>
        /// Simple write to the device.
        /// </summary>
        /// <param name="memoryAddress">Register address to write.</param>
        /// <param name="value">Byte to write.</param>
        public static void Write(I2CDevice device, byte memoryAddress, byte value, int timeout = DefaultTimeout)
        {
            I2CDevice.I2CTransaction[] transaction;
            transaction = new I2CDevice.I2CTransaction[]
            {
                I2CDevice.CreateWriteTransaction(new byte[] { memoryAddress, value })
            };
            int result = Execute(device, transaction, timeout);
        }

        /// <summary>
        /// Simple write to the device.
        /// </summary>
        /// <param name="memoryAddress">Register address to write.</param>
        /// <param name="value">Byte to write.</param>
        public static void Write(I2CDevice device, byte memoryAddress, byte[] values, int timeout = DefaultTimeout)
        {
            I2CDevice.I2CTransaction[] transaction;
            byte[] buffer = new byte[values.Length + 1];
            buffer[0] = memoryAddress;
            for (int i = 1; i < values.Length; i++)
                buffer[i] = values[i - 1];

            transaction = new I2CDevice.I2CTransaction[]
            {
                I2CDevice.CreateWriteTransaction(buffer)
            };
            int result = Execute(device, transaction, timeout);
        }

        /// <summary>
        /// Simple write to the device.
        /// </summary>
        /// <param name="value">Byte to write.</param>
        public static int Write(I2CDevice device, byte value, int timeout = DefaultTimeout)
        {
            I2CDevice.I2CTransaction[] transaction;
            transaction = new I2CDevice.I2CTransaction[]
            {
                I2CDevice.CreateWriteTransaction(new byte[] { value })
            };
            int result = Execute(device, transaction, timeout);
            return result;
        }

        /// <summary>
        /// Simple write to the device.
        /// </summary>
        /// <param name="value">Bytes to write.</param>
        public static int Write(I2CDevice device, byte[] values, int timeout = DefaultTimeout)
        {
            I2CDevice.I2CTransaction[] transaction;
            transaction = new I2CDevice.I2CTransaction[]
            {
                I2CDevice.CreateWriteTransaction(values)
            };
            int result = Execute(device, transaction, timeout);
            return result;
        }
    }
}
