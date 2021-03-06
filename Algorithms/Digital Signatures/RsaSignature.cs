﻿using CiphersLibrary.Core;
using CiphersLibrary.Data;
using CiphersLibrary.Factories;
using System;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace CiphersLibrary.Algorithms
{
    public class RsaSignature : IDigitalSignatureAlgorithm<Tuple<BigInteger, BigInteger>>
    {
        #region Ctor

        protected RsaSignature()
        {

        }
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="p">First prime number</param>
        /// <param name="q">Second prime number</param>
        /// <param name="privateKey">Private key d</param>
        /// <param name="hashFunction">hashfunction to use in algorithm</param>
        /// <param name="ChecksNeeded">check if data is right </param>
        public RsaSignature(BigInteger p, BigInteger q, BigInteger privateKey, HashFunction hashFunction, bool ChecksNeeded)
        {
            if (ChecksNeeded)
            {
                if ((!p.CheckIfPrime()) || (!q.CheckIfPrime()))
                    throw new ArgumentException("p and q must be prime numbers!");
                //if privatekey = 1 then signature == h 
                //if privateKey == phi(n) - 1; then s == 1   
                //if privateKey > phi(n) then there'll problem with multipl. inversion in equation d*e = 1 mod phi(n)
                if ((privateKey < 2) || (privateKey > (p - 1) * (q - 1) - 1))
                    throw new ArgumentException("Private key should be 2 < < (p - 1) * (q - 1) - 1)!");
                //We need private key to be prime with phi(n) to find open key later 
                if (!NumericAlgorithms.GCD(privateKey, (p - 1) * (q - 1)).Equals(1))
                    throw new ArgumentException("D should be mutually prime with (p - 1) * (q - 1)");
            }

            Modulus = p * q;

            PublicExp = NumericAlgorithms.ExtendedGCD((p - 1) * (q - 1), privateKey);

            PrivateExp = privateKey;

            HashFunc = (hashFunction == HashFunction.MD5) ? null : new HashFunctionFactory().NewHashFunction(Modulus, hashFunction);
        }

        #endregion

        #region Private props

        private BigInteger Modulus { get; set; } = 11 * 19;

        private BigInteger PrivateExp { get; set; } = 67;

        private BigInteger PublicExp { get; set; } = 43;

        private IHashFunction HashFunc { get; set; }

        #endregion

        /// <summary>
        /// Method for signing the file with RSA algorithm
        /// </summary>
        /// <param name="compressedMessage"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public virtual Tuple<BigInteger,BigInteger> Sign(string fileName)
        {
            var messageToCompress = File.ReadAllBytes(fileName);

            var compressedMessage = Digest(messageToCompress);

            var result = HashImage(compressedMessage, PrivateExp);

            using (var fs = new FileStream(GetOutputSignedPath(fileName), FileMode.Create))
            using (var sw = new StreamWriter(fs))
            {
                sw.Write(result.ToString());
            }

            return new Tuple<BigInteger, BigInteger>(result, compressedMessage);
        }

        public virtual bool CheckSignature(string fileName)
        {
            try
            {
                var signature = File.ReadAllText(GetOutputSignedPath(fileName));
                var textToAnalyze = File.ReadAllText(fileName);

                if (string.IsNullOrWhiteSpace(signature))
                    throw new ArgumentException("Wrong file to analyze!");

                if (!BigInteger.TryParse(signature, out var signatureInt))
                {
                    throw new ArgumentException("Wrong signature.");
                }

                return Digest(Encoding.Default.GetBytes(textToAnalyze)) == HashImage(signatureInt, PublicExp);
            }
            catch (IOException) { throw; }
            catch (ArgumentException) { throw; }

        }

        /// <summary>
        /// Generates hash of the message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="startHash"></param>
        /// <returns></returns>
        protected virtual BigInteger Digest(byte[] message)
        {
            return (HashFunc == null) ? new BigInteger(MD5.Create().ComputeHash(message)) : HashFunc.IntHash(message);
        }

        /// <summary>
        /// Hash "image" used in RSA digital signature algorithm
        /// </summary>
        /// <param name="message"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        protected virtual BigInteger HashImage(BigInteger message, BigInteger key)
        {
            if (((message > 0) ? message : -message) >= Modulus)
                throw new ArgumentException("Modulus p * q was too small.");

            return NumericAlgorithms.FastExp(message, key, Modulus);
        }

        #region Helper methods

        internal static string GetOutputSignedPath(string InputPath)
        {
            return InputPath.Insert(InputPath.LastIndexOf('.'), "Signed");
        }

        #endregion


    }
}
