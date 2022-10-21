using System;

namespace Tests.Utils
{
    internal static class Tools
    {
        public static void ExceptionHandler(Exception e)
        {
            // In unit tests, rethrow the exception
            throw e;
        }
    }
}