using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Selenium.Spotfire.TestHelpers;

namespace Selenium.Spotfire.Tests
{
    [TestClass]
    public class SpotfireWrapperServerTest
    { 
        [TestMethod]
        public void SpotfireWrapperHappyTrail()
        {
            MultipleAsserts checks = new MultipleAsserts();

            SpotfireWrapperServer.StartupServer();
            checks.CheckErrors(() => Assert.AreEqual("8080", SpotfireWrapperServer.Port, "We expect the port number on the started server to be 8080"));
            // We perhaps should test making requests, but that's really covered by the main tests so we won't worry here...
            SpotfireWrapperServer.StopServer();

            checks.AssertEmpty();
        }

        [TestMethod]
        public void MultipleReferences()
        {
            MultipleAsserts checks = new MultipleAsserts();

            SpotfireWrapperServer.StartupServer();
            SpotfireWrapperServer.StartupServer();
            checks.CheckErrors(() => Assert.AreEqual("8080", SpotfireWrapperServer.Port, "We expect the port number on the started server to be 8080"));
            // We perhaps should test making requests, but that's really covered by the main tests so we won't worry here...
            SpotfireWrapperServer.StopServer();
            SpotfireWrapperServer.StopServer();

            checks.AssertEmpty();
        }

        [TestMethod]
        public void PortInUse()
        {
            MultipleAsserts checks = new MultipleAsserts();

            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(@"http://+:8080/");
            listener.Start();

            SpotfireWrapperServer.StartupServer();
            checks.CheckErrors(() => Assert.AreNotEqual("8080", SpotfireWrapperServer.Port, "We expect the port number on the started server to be something other than 8080"));
            // We perhaps should test making requests, but that's really covered by the main tests so we won't worry here...
            SpotfireWrapperServer.StopServer();

            listener.Stop();

            checks.AssertEmpty();
        }

        [TestMethod]
        public void AllPortsInUse()
        {
            MultipleAsserts checks = new MultipleAsserts();

            List<HttpListener> listeners = new List<HttpListener>();
            int portNumber = 8080;
            while (portNumber <= 9000)
            {
                HttpListener listener = new HttpListener();
                listener.Prefixes.Add(@"http://+:" + portNumber.ToString() + "/");
                try
                {
                    listener.Start();
                    listeners.Add(listener);
                }
                catch
                {
                    // Ignore - something is using the port
                }
                portNumber++;
            }

            try
            {
                SpotfireWrapperServer.StartupServer();
                checks.CheckErrors(() => Assert.Fail("We expect an error because all our potential ports are in user"));
            }
            catch
            {
                // ignore
            }

            foreach (HttpListener listener in listeners)
            {
                listener.Stop();
            }

            checks.AssertEmpty();
        }
    }
}
