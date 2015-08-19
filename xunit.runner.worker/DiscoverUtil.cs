﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using xunit.runner.data;

namespace xunit.runner.worker
{
    internal sealed class DiscoverUtil
    {
        private sealed class Impl : TestMessageVisitor<IDiscoveryCompleteMessage>
        {
            private readonly ITestFrameworkDiscoverer _discoverer;
            private readonly BinaryWriter _writer;

            internal Impl(ITestFrameworkDiscoverer discoverer, BinaryWriter writer)
            {
                _discoverer = discoverer;
                _writer = writer;
            }

            protected override bool Visit(ITestCaseDiscoveryMessage testCaseDiscovered)
            {
                var testCase = testCaseDiscovered.TestCase;
                var testCaseData = new TestCaseData(
                    _discoverer.Serialize(testCase),
                    testCase.DisplayName,
                    testCaseDiscovered.TestAssembly.Assembly.AssemblyPath);

                testCaseData.WriteTo(_writer);
                return true;
            }
        }

        internal static void Go(string fileName, Stream stream)
        {
            using (AssemblyHelper.SubscribeResolve())
            using (var xunit = new XunitFrontController(
                useAppDomain: true,
                assemblyFileName: fileName,
                diagnosticMessageSink: new MessageVisitor(),
                shadowCopy: false))
            using (var writer = new BinaryWriter(stream, Constants.Encoding, leaveOpen: true))
            using (var impl = new Impl(xunit, writer))
            {
                xunit.Find(includeSourceInformation: false, messageSink: impl, discoveryOptions: TestFrameworkOptions.ForDiscovery());
                impl.Finished.WaitOne();
            }
        }
    }
}
