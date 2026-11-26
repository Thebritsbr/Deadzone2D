// ShopPrices.cs — Preços do sistema de moedas (DeadZoneCoins) de cada arma.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Deadzone2D
{
    public static class ShopPrices
    {
        public static readonly Dictionary<string, int> Prices = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Glock"] = 40,
            ["MP40"] = 55,
            ["AK47"] = 80,
            ["SniperAWM"] = 200,
            ["MiniUZI"] = 70,
        };

        public static string ConfigDir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");

        public static void Save(string id, int price)
        {
            try
            {
                var p = Path.Combine(ConfigDir, "ShopPrices.json");
                Prices[id] = price;
                using var doc = new Utf8JsonWriter(File.Create(p));
                doc.WriteStartObject();
                foreach (var kv in Prices)
                {
                    doc.WriteNumber(kv.Key, kv.Value);
                }
                doc.WriteEndObject();
            }
            catch { }
        }

        public static int Get(string id) => Prices.TryGetValue(id ?? "", out var p) ? p : 100;
    }
}
