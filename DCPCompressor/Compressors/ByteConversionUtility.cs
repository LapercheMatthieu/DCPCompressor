using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

    namespace DCPCompressor.Compressors
    {
        /// <summary>
        /// Classe utilitaire optimisée pour les conversions entre valeurs numériques et octets
        /// Utilise des techniques avancées pour maximiser les performances
        /// </summary>
        public static class ByteConversionUtility
        {
            // Dictionnaire de fonctions d'écriture pour chaque type
            private static readonly Dictionary<Type, Action<double, byte[], int>> _valueWriters;

            // Dictionnaire de fonctions de lecture pour chaque type
            private static readonly Dictionary<Type, Func<byte[], int, double>> _valueReaders;

            // Types de base supportés (pour vérification rapide)
            private static readonly HashSet<Type> _supportedTypes;

            // Initialisation statique des dictionnaires
            static ByteConversionUtility()
            {
                _supportedTypes = new HashSet<Type>
            {
                typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
                typeof(int), typeof(uint), typeof(long), typeof(ulong),
                typeof(float), typeof(double)
            };

                _valueWriters = new Dictionary<Type, Action<double, byte[], int>>
            {
                { typeof(byte), (value, buffer, offset) => buffer[offset] = (byte)value },
                { typeof(sbyte), (value, buffer, offset) => buffer[offset] = (byte)((sbyte)value) },
                { typeof(short), WriteShort },
                { typeof(ushort), WriteUshort },
                { typeof(int), WriteInt },
                { typeof(uint), WriteUint },
                { typeof(long), WriteLong },
                { typeof(ulong), WriteUlong },
                { typeof(float), WriteFloat },
                { typeof(double), WriteDouble }
            };

                _valueReaders = new Dictionary<Type, Func<byte[], int, double>>
            {
                { typeof(byte), (buffer, offset) => buffer[offset] },
                { typeof(sbyte), (buffer, offset) => (sbyte)buffer[offset] },
                { typeof(short), ReadShort },
                { typeof(ushort), ReadUshort },
                { typeof(int), ReadInt },
                { typeof(uint), ReadUint },
                { typeof(long), ReadLong },
                { typeof(ulong), ReadUlong },
                { typeof(float), ReadFloat },
                { typeof(double), ReadDouble }
            };
            }

            /// <summary>
            /// Écrit une valeur dans un tableau d'octets selon le type spécifié (version optimisée)
            /// </summary>
           // [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void WriteValueToBuffer(double value, Type type, byte[] buffer, int offset)
            {
                // Vérification rapide pour les types les plus courants
                if (type == typeof(int))
                {
                    WriteInt(value, buffer, offset);
                    return;
                }
                if (type == typeof(float))
                {
                    WriteFloat(value, buffer, offset);
                    return;
                }
                if (type == typeof(double))
                {
                    WriteDouble(value, buffer, offset);
                    return;
                }

                // Pour les autres types, utiliser le dictionnaire
                if (_valueWriters.TryGetValue(type, out var writer))
                {
                    writer(value, buffer, offset);
                }
                else
                {
                    // Cas par défaut - utiliser double
                    WriteDouble(value, buffer, offset);
                }
            }

            /// <summary>
            /// Écrit une valeur dans une liste d'octets selon le type spécifié (version optimisée)
            /// </summary>
         //   [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void WriteValueToBuffer(double value, Type type, List<byte> buffer, int offset)
            {
                // S'assurer que la liste a une taille suffisante
                int bytesNeeded = GetBytesForType(type);
                EnsureListCapacity(buffer, offset + bytesNeeded);

                // Vérification rapide des types les plus courants
                if (type == typeof(byte))
                {
                    buffer[offset] = (byte)value;
                    return;
                }
                if (type == typeof(int))
                {
                    WriteIntToList(value, buffer, offset);
                    return;
                }
                if (type == typeof(double))
                {
                    WriteDoubleToList(value, buffer, offset);
                    return;
                }

                // Pour les autres types
                if (type == typeof(sbyte))
                {
                    buffer[offset] = (byte)((sbyte)value);
                }
                else if (type == typeof(short))
                {
                    WriteShortToList(value, buffer, offset);
                }
                else if (type == typeof(ushort))
                {
                    WriteUshortToList(value, buffer, offset);
                }
                else if (type == typeof(uint))
                {
                    WriteUintToList(value, buffer, offset);
                }
                else if (type == typeof(long))
                {
                    WriteLongToList(value, buffer, offset);
                }
                else if (type == typeof(ulong))
                {
                    WriteUlongToList(value, buffer, offset);
                }
                else if (type == typeof(float))
                {
                    WriteFloatToList(value, buffer, offset);
                }
                else
                {
                    // Par défaut - traiter comme un double
                    WriteDoubleToList(value, buffer, offset);
                }
            }

            /// <summary>
            /// Écrit une valeur dans un segment de mémoire (version haute performance)
            /// </summary>
          //  [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void WriteValueToSpan(double value, Type type, Span<byte> buffer, int offset)
            {
                if (type == typeof(byte))
                {
                    buffer[offset] = (byte)value;
                }
                else if (type == typeof(sbyte))
                {
                    buffer[offset] = (byte)((sbyte)value);
                }
                else if (type == typeof(short))
                {
                    short shortVal = (short)value;
                    buffer[offset] = (byte)shortVal;
                    buffer[offset + 1] = (byte)(shortVal >> 8);
                }
                else if (type == typeof(ushort))
                {
                    ushort ushortVal = (ushort)value;
                    buffer[offset] = (byte)ushortVal;
                    buffer[offset + 1] = (byte)(ushortVal >> 8);
                }
                else if (type == typeof(int))
                {
                    int intVal = (int)value;
                    buffer[offset] = (byte)intVal;
                    buffer[offset + 1] = (byte)(intVal >> 8);
                    buffer[offset + 2] = (byte)(intVal >> 16);
                    buffer[offset + 3] = (byte)(intVal >> 24);
                }
                else if (type == typeof(uint))
                {
                    uint uintVal = (uint)value;
                    buffer[offset] = (byte)uintVal;
                    buffer[offset + 1] = (byte)(uintVal >> 8);
                    buffer[offset + 2] = (byte)(uintVal >> 16);
                    buffer[offset + 3] = (byte)(uintVal >> 24);
                }
                else if (type == typeof(long))
                {
                    long longVal = (long)value;
                    buffer[offset] = (byte)longVal;
                    buffer[offset + 1] = (byte)(longVal >> 8);
                    buffer[offset + 2] = (byte)(longVal >> 16);
                    buffer[offset + 3] = (byte)(longVal >> 24);
                    buffer[offset + 4] = (byte)(longVal >> 32);
                    buffer[offset + 5] = (byte)(longVal >> 40);
                    buffer[offset + 6] = (byte)(longVal >> 48);
                    buffer[offset + 7] = (byte)(longVal >> 56);
                }
                else if (type == typeof(ulong))
                {
                    ulong ulongVal = (ulong)value;
                    buffer[offset] = (byte)ulongVal;
                    buffer[offset + 1] = (byte)(ulongVal >> 8);
                    buffer[offset + 2] = (byte)(ulongVal >> 16);
                    buffer[offset + 3] = (byte)(ulongVal >> 24);
                    buffer[offset + 4] = (byte)(ulongVal >> 32);
                    buffer[offset + 5] = (byte)(ulongVal >> 40);
                    buffer[offset + 6] = (byte)(ulongVal >> 48);
                    buffer[offset + 7] = (byte)(ulongVal >> 56);
                }
                else if (type == typeof(float))
                {
                    // Utiliser la manipulation de bits pour éviter la copie
                    uint bits = BitConverter.SingleToUInt32Bits((float)value);
                    buffer[offset] = (byte)bits;
                    buffer[offset + 1] = (byte)(bits >> 8);
                    buffer[offset + 2] = (byte)(bits >> 16);
                    buffer[offset + 3] = (byte)(bits >> 24);
                }
                else if (type == typeof(double))
                {
                    // Utiliser la manipulation de bits pour éviter la copie
                    ulong bits = BitConverter.DoubleToUInt64Bits(value);
                    buffer[offset] = (byte)bits;
                    buffer[offset + 1] = (byte)(bits >> 8);
                    buffer[offset + 2] = (byte)(bits >> 16);
                    buffer[offset + 3] = (byte)(bits >> 24);
                    buffer[offset + 4] = (byte)(bits >> 32);
                    buffer[offset + 5] = (byte)(bits >> 40);
                    buffer[offset + 6] = (byte)(bits >> 48);
                    buffer[offset + 7] = (byte)(bits >> 56);
                }
                else
                {
                    // Par défaut, traiter comme un double
                    ulong bits = BitConverter.DoubleToUInt64Bits(value);
                    buffer[offset] = (byte)bits;
                    buffer[offset + 1] = (byte)(bits >> 8);
                    buffer[offset + 2] = (byte)(bits >> 16);
                    buffer[offset + 3] = (byte)(bits >> 24);
                    buffer[offset + 4] = (byte)(bits >> 32);
                    buffer[offset + 5] = (byte)(bits >> 40);
                    buffer[offset + 6] = (byte)(bits >> 48);
                    buffer[offset + 7] = (byte)(bits >> 56);
                }
            }

            /// <summary>
            /// Lit une valeur depuis un tableau d'octets selon le type spécifié (version optimisée)
            /// </summary>
         //   [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double ReadValueFromBuffer(byte[] buffer, int offset, Type type)
            {
                // Vérification rapide pour les types les plus courants
                if (type == typeof(int))
                {
                    return ReadInt(buffer, offset);
                }
                if (type == typeof(float))
                {
                    return ReadFloat(buffer, offset);
                }
                if (type == typeof(double))
                {
                    return ReadDouble(buffer, offset);
                }

                // Pour les autres types, utiliser le dictionnaire
                if (_valueReaders.TryGetValue(type, out var reader))
                {
                    return reader(buffer, offset);
                }
                else
                {
                    // Cas par défaut - utiliser int
                    return ReadInt(buffer, offset);
                }
            }

            /// <summary>
            /// Obtient la taille en octets d'un type donné (version optimisée)
            /// </summary>
        //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int GetBytesForType(Type type)
            {
                if (type == typeof(byte) || type == typeof(sbyte))
                    return 1;
                if (type == typeof(short) || type == typeof(ushort))
                    return 2;
                if (type == typeof(int) || type == typeof(uint) || type == typeof(float))
                    return 4;
                if (type == typeof(long) || type == typeof(ulong) || type == typeof(double))
                    return 8;

                return 8; // Par défaut, utiliser double
            }

            /// <summary>
            /// S'assure que la liste a une capacité suffisante (version optimisée)
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void EnsureListCapacity(List<byte> list, int requiredSize)
            {
                int currentSize = list.Count;
                if (currentSize >= requiredSize)
                    return;

                // Optimisation: préallouer avec une croissance exponentielle
                if (list.Capacity < requiredSize)
                {
                    // Stratégie de croissance: doubler la capacité ou utiliser la taille requise si plus grande
                    int newCapacity = Math.Max(list.Capacity * 2, requiredSize);
                    list.Capacity = newCapacity;
                }

                // Ajouter uniquement le nombre d'octets nécessaires
                int toAdd = requiredSize - currentSize;
                for (int i = 0; i < toAdd; i++)
                {
                    list.Add(0);
                }
            }

            #region Méthodes d'écriture optimisées pour List<byte>

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void WriteShortToList(double value, List<byte> buffer, int offset)
            {
                short shortVal = (short)value;
                buffer[offset] = (byte)shortVal;
                buffer[offset + 1] = (byte)(shortVal >> 8);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void WriteUshortToList(double value, List<byte> buffer, int offset)
            {
                ushort ushortVal = (ushort)value;
                buffer[offset] = (byte)ushortVal;
                buffer[offset + 1] = (byte)(ushortVal >> 8);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void WriteIntToList(double value, List<byte> buffer, int offset)
            {
                int intVal = (int)value;
                buffer[offset] = (byte)intVal;
                buffer[offset + 1] = (byte)(intVal >> 8);
                buffer[offset + 2] = (byte)(intVal >> 16);
                buffer[offset + 3] = (byte)(intVal >> 24);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void WriteUintToList(double value, List<byte> buffer, int offset)
            {
                uint uintVal = (uint)value;
                buffer[offset] = (byte)uintVal;
                buffer[offset + 1] = (byte)(uintVal >> 8);
                buffer[offset + 2] = (byte)(uintVal >> 16);
                buffer[offset + 3] = (byte)(uintVal >> 24);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void WriteLongToList(double value, List<byte> buffer, int offset)
            {
                long longVal = (long)value;
                buffer[offset] = (byte)longVal;
                buffer[offset + 1] = (byte)(longVal >> 8);
                buffer[offset + 2] = (byte)(longVal >> 16);
                buffer[offset + 3] = (byte)(longVal >> 24);
                buffer[offset + 4] = (byte)(longVal >> 32);
                buffer[offset + 5] = (byte)(longVal >> 40);
                buffer[offset + 6] = (byte)(longVal >> 48);
                buffer[offset + 7] = (byte)(longVal >> 56);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void WriteUlongToList(double value, List<byte> buffer, int offset)
            {
                ulong ulongVal = (ulong)value;
                buffer[offset] = (byte)ulongVal;
                buffer[offset + 1] = (byte)(ulongVal >> 8);
                buffer[offset + 2] = (byte)(ulongVal >> 16);
                buffer[offset + 3] = (byte)(ulongVal >> 24);
                buffer[offset + 4] = (byte)(ulongVal >> 32);
                buffer[offset + 5] = (byte)(ulongVal >> 40);
                buffer[offset + 6] = (byte)(ulongVal >> 48);
                buffer[offset + 7] = (byte)(ulongVal >> 56);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void WriteFloatToList(double value, List<byte> buffer, int offset)
            {
                uint bits = BitConverter.SingleToUInt32Bits((float)value);
                buffer[offset] = (byte)bits;
                buffer[offset + 1] = (byte)(bits >> 8);
                buffer[offset + 2] = (byte)(bits >> 16);
                buffer[offset + 3] = (byte)(bits >> 24);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void WriteDoubleToList(double value, List<byte> buffer, int offset)
            {
                ulong bits = BitConverter.DoubleToUInt64Bits(value);
                buffer[offset] = (byte)bits;
                buffer[offset + 1] = (byte)(bits >> 8);
                buffer[offset + 2] = (byte)(bits >> 16);
                buffer[offset + 3] = (byte)(bits >> 24);
                buffer[offset + 4] = (byte)(bits >> 32);
                buffer[offset + 5] = (byte)(bits >> 40);
                buffer[offset + 6] = (byte)(bits >> 48);
                buffer[offset + 7] = (byte)(bits >> 56);
            }

            #endregion

            #region Fonctions d'écriture spécifiques aux types (optimisées)

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void WriteShort(double value, byte[] buffer, int offset)
            {
                short shortVal = (short)value;
                buffer[offset] = (byte)shortVal;
                buffer[offset + 1] = (byte)(shortVal >> 8);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void WriteUshort(double value, byte[] buffer, int offset)
            {
                ushort ushortVal = (ushort)value;
                buffer[offset] = (byte)ushortVal;
                buffer[offset + 1] = (byte)(ushortVal >> 8);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void WriteInt(double value, byte[] buffer, int offset)
            {
                int intVal = (int)value;
                buffer[offset] = (byte)intVal;
                buffer[offset + 1] = (byte)(intVal >> 8);
                buffer[offset + 2] = (byte)(intVal >> 16);
                buffer[offset + 3] = (byte)(intVal >> 24);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void WriteUint(double value, byte[] buffer, int offset)
            {
                uint uintVal = (uint)value;
                buffer[offset] = (byte)uintVal;
                buffer[offset + 1] = (byte)(uintVal >> 8);
                buffer[offset + 2] = (byte)(uintVal >> 16);
                buffer[offset + 3] = (byte)(uintVal >> 24);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void WriteLong(double value, byte[] buffer, int offset)
            {
                long longVal = (long)value;
                buffer[offset] = (byte)longVal;
                buffer[offset + 1] = (byte)(longVal >> 8);
                buffer[offset + 2] = (byte)(longVal >> 16);
                buffer[offset + 3] = (byte)(longVal >> 24);
                buffer[offset + 4] = (byte)(longVal >> 32);
                buffer[offset + 5] = (byte)(longVal >> 40);
                buffer[offset + 6] = (byte)(longVal >> 48);
                buffer[offset + 7] = (byte)(longVal >> 56);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void WriteUlong(double value, byte[] buffer, int offset)
            {
                ulong ulongVal = (ulong)value;
                buffer[offset] = (byte)ulongVal;
                buffer[offset + 1] = (byte)(ulongVal >> 8);
                buffer[offset + 2] = (byte)(ulongVal >> 16);
                buffer[offset + 3] = (byte)(ulongVal >> 24);
                buffer[offset + 4] = (byte)(ulongVal >> 32);
                buffer[offset + 5] = (byte)(ulongVal >> 40);
                buffer[offset + 6] = (byte)(ulongVal >> 48);
                buffer[offset + 7] = (byte)(ulongVal >> 56);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void WriteFloat(double value, byte[] buffer, int offset)
            {
                // Utiliser le bitshift au lieu de BitConverter.GetBytes pour éviter l'allocation
                uint bits = BitConverter.SingleToUInt32Bits((float)value);
                buffer[offset] = (byte)bits;
                buffer[offset + 1] = (byte)(bits >> 8);
                buffer[offset + 2] = (byte)(bits >> 16);
                buffer[offset + 3] = (byte)(bits >> 24);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void WriteDouble(double value, byte[] buffer, int offset)
            {
                // Utiliser le bitshift au lieu de BitConverter.GetBytes pour éviter l'allocation
                ulong bits = BitConverter.DoubleToUInt64Bits(value);
                buffer[offset] = (byte)bits;
                buffer[offset + 1] = (byte)(bits >> 8);
                buffer[offset + 2] = (byte)(bits >> 16);
                buffer[offset + 3] = (byte)(bits >> 24);
                buffer[offset + 4] = (byte)(bits >> 32);
                buffer[offset + 5] = (byte)(bits >> 40);
                buffer[offset + 6] = (byte)(bits >> 48);
                buffer[offset + 7] = (byte)(bits >> 56);
            }

            #endregion

            #region Fonctions de lecture spécifiques aux types (optimisées)

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static double ReadShort(byte[] buffer, int offset)
            {
                return (short)(buffer[offset] | (buffer[offset + 1] << 8));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static double ReadUshort(byte[] buffer, int offset)
            {
                return (ushort)(buffer[offset] | (buffer[offset + 1] << 8));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static double ReadInt(byte[] buffer, int offset)
            {
                return buffer[offset] |
                      (buffer[offset + 1] << 8) |
                      (buffer[offset + 2] << 16) |
                      (buffer[offset + 3] << 24);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static double ReadUint(byte[] buffer, int offset)
            {
                return (uint)(buffer[offset] |
                             (buffer[offset + 1] << 8) |
                             (buffer[offset + 2] << 16) |
                             (buffer[offset + 3] << 24));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static double ReadLong(byte[] buffer, int offset)
            {
                return (long)buffer[offset] |
                      ((long)buffer[offset + 1] << 8) |
                      ((long)buffer[offset + 2] << 16) |
                      ((long)buffer[offset + 3] << 24) |
                      ((long)buffer[offset + 4] << 32) |
                      ((long)buffer[offset + 5] << 40) |
                      ((long)buffer[offset + 6] << 48) |
                      ((long)buffer[offset + 7] << 56);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static double ReadUlong(byte[] buffer, int offset)
            {
                return (ulong)buffer[offset] |
                      ((ulong)buffer[offset + 1] << 8) |
                      ((ulong)buffer[offset + 2] << 16) |
                      ((ulong)buffer[offset + 3] << 24) |
                      ((ulong)buffer[offset + 4] << 32) |
                      ((ulong)buffer[offset + 5] << 40) |
                      ((ulong)buffer[offset + 6] << 48) |
                      ((ulong)buffer[offset + 7] << 56);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static double ReadFloat(byte[] buffer, int offset)
            {
                // Éviter l'allocation en utilisant le bitshift inverse
                uint bits = (uint)buffer[offset] |
                            ((uint)buffer[offset + 1] << 8) |
                            ((uint)buffer[offset + 2] << 16) |
                            ((uint)buffer[offset + 3] << 24);
                return BitConverter.Int32BitsToSingle((int)bits);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static double ReadDouble(byte[] buffer, int offset)
            {
                // Éviter l'allocation en utilisant le bitshift inverse
                ulong bits = (ulong)buffer[offset] |
                             ((ulong)buffer[offset + 1] << 8) |
                             ((ulong)buffer[offset + 2] << 16) |
                             ((ulong)buffer[offset + 3] << 24) |
                             ((ulong)buffer[offset + 4] << 32) |
                             ((ulong)buffer[offset + 5] << 40) |
                             ((ulong)buffer[offset + 6] << 48) |
                             ((ulong)buffer[offset + 7] << 56);
                return BitConverter.Int64BitsToDouble((long)bits);
            }

            #endregion
        }
    }

