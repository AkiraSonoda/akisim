/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

using NUnit.Framework;
using OpenSim.Tests.Common;

namespace OpenSim.Framework.Serialization.Tests
{
    [TestFixture]
	public class AgentCapsTest : OpenSimTestCase
    {
        [SetUp]
        public virtual void SetUp()
        {
//            TestHelpers.InMethod();
            // Disable logging for each test so that one where logging is enabled doesn't cause all subsequent tests
            // to have logging on if it failed with an exception.
            TestHelpers.EnableLogging();
        }

		[Test]
		public void SerializeMapTest() {
			OSDMap capsMap = new OSDMap();
			capsMap.Add("FetchInventoryDescendents2", "d5cb3302-2db8-4881-94de-161ce09df1a6");
			capsMap.Add("FetchInventory2", "e1563faf-a081-4410-b160-ea1639aef192");
			capsMap.Add("GetTexture", "26cecb27-ea2c-4152-b319-d06dd0b516ae");
			capsMap.Add("GetMesh", "24f57f22-59b0-4c4a-a2c2-95a8858de972");

			String result = OSDParser.SerializeJsonString(capsMap);
			String jsonString = "{\"FetchInventoryDescendents2\":\"d5cb3302-2db8-4881-94de-161ce09df1a6\",\"FetchInventory2\":\"e1563faf-a081-4410-b160-ea1639aef192\",\"GetTexture\":\"26cecb27-ea2c-4152-b319-d06dd0b516ae\",\"GetMesh\":\"24f57f22-59b0-4c4a-a2c2-95a8858de972\"}";
			Assert.That(jsonString.Equals(result));

		}
	}
}

