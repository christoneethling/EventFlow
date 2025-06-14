using System;

namespace EventFlow.Examples.Shipping.Tests;

public static class DateTimeExtensions
{
    public static DateTime October(this int day, int year) => new(year, 10, day);
    public static DateTime November(this int day, int year) => new(year, 11, day);
    public static DateTime January(this int day, int year) => new(year, 1, day);
        
    public static DateTime At(this DateTime date, int hours, int minutes) => new(date.Year, date.Month, date.Day, hours, minutes, 0);
}