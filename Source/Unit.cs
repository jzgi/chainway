﻿using ChainFx;

namespace ChainSmart;

public class Unit
{
    public static readonly Map<string, short> Typs = new()
    {
        { "两", 50 },
        { "斤", 500 },
        { "公斤", 1000 },
        { "个", 0 },
        { "只", 0 },
        { "根", 0 },
        { "份", 0 },
        { "包", 0 },
        { "瓶", 0 },
        { "桶", 0 },
        { string.Empty, 0 },
        { "位", 0 },
        { "单", 0 },
        { "间天", 0 },
        { "天", 0 },
    };

    public static readonly Map<short, string> Metrics = new()
    {
        { 0, "未定义" },
        { 50, "1两" },
        { 100, "2两" },
        { 150, "3两" },
        { 200, "4两" },
        { 250, "5两" },
        { 300, "6两" },
        { 350, "7两" },
        { 400, "8两" },
        { 450, "9两" },
        { 500, "1斤" },
        { 750, "1.5斤" },
        { 1000, "2斤" },
        { 1500, "3斤" },
        { 2000, "4斤" },
        { 2500, "5斤" },
        { 3000, "6斤" },
        { 3500, "7斤" },
        { 4000, "8斤" },
        { 4500, "9斤" },
        { 5000, "10斤" },
        { 5500, "11斤" },
        { 6000, "12斤" },
        { 6500, "13斤" },
        { 7000, "14斤" },
        { 7500, "15斤" },
        { 8000, "16斤" },
        { 8500, "17斤" },
        { 9000, "18斤" },
        { 9500, "19斤" },
        { 10000, "20斤" },
        { 10500, "21斤" },
        { 11000, "22斤" },
        { 11500, "23斤" },
        { 12000, "24斤" },
        { 12500, "25斤" },
        { 13000, "26斤" },
        { 13500, "27斤" },
        { 14000, "28斤" },
        { 14500, "29斤" },
        { 15000, "30斤" },
    };

    public static int Convert(string u1, int v, string u2, short pieceful)
    {
        return 0;
    }
}