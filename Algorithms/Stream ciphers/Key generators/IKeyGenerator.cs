﻿using System;
using CiphersLibrary.Data;

namespace CiphersLibrary.Algorithms
{
    public interface IKeyGenerator
    {
        byte[] GenerateKey();

        bool Initialize(string InitialState, long bytesMessageLength);
    }
} 
