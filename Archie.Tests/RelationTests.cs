﻿//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Runtime.CompilerServices;
//using System.Runtime.InteropServices;
//using System.Text;
//using System.Threading.Tasks;

//namespace Archie.Tests
//{
//    internal class RelationTests
//    {
//        ArchetypeDefinition def = ArchetypeBuilder.Create().Relation<RelSS>().Inc<Component1>().Inc<Component2>().Inc<Component3>().End();

//        World world;

//        [SetUp]
//        public void Init()
//        {
//            world = new World();
//        }

//        [Test]
//        public void GetRelationDataSingleTest()
//        {
//            var ent = world.CreateEntityImmediate();
//            var ent2 = world.CreateEntityImmediate();
//            ent.AddRelationTarget(ent2, new RelSS(1337));

//            Assert.AreEqual(ent.GetRelationData<RelSS>(ent2), new RelSS(1337));
//        }

//        [Test]
//        public void GetRelationTargetSingleTest()
//        {
//            var ent = world.CreateEntityImmediate();
//            var ent2 = world.CreateEntityImmediate();
//            ent.AddRelationTarget(ent2, new RelSS(1337));

//            Assert.AreEqual(ent.GetRelationTarget<RelSS>(), ent2);
//        }

//        [Test]
//        public void GetRelationTargetsMultiTest()
//        {
//            var ent = world.CreateEntityImmediate();
//            var ent2 = world.CreateEntityImmediate();
//            var ent3 = world.CreateEntityImmediate();
//            var ent4 = world.CreateEntityImmediate();
//            ent.AddRelationTarget(ent2, new RelSM(1337));
//            ent.AddRelationTarget<RelSM>(ent3);
//            ent.AddRelationTarget<RelSM>(ent4);
//            ReadOnlySpan<Entity> targets = ent.GetRelationTargets<RelSM>();
//            Span<Entity> entities = stackalloc Entity[3] { ent2, ent3, ent4 };
//            Assert.AreEqual(entities.Length, targets.Length);
//            for (int i = 0; i < targets.Length; i++)
//            {
//                Assert.AreEqual(entities[i], targets[i]);
//            }
//        }

//        [Test]
//        public void GetRelationDataMultiTest()
//        {
//            var ent = world.CreateEntityImmediate();
//            var ent2 = world.CreateEntityImmediate();
//            var ent3 = world.CreateEntityImmediate();
//            var ent4 = world.CreateEntityImmediate();
//            ent.AddRelationTarget(ent2, new RelMM(1));
//            ent.AddRelationTarget<RelMM>(ent3, new RelMM(2));
//            ent.AddRelationTarget<RelMM>(ent4, new RelMM(3));
//            Span<RelMM> relationData = ent.GetRelationData<RelMM>();
//            Span<RelMM> data = MemoryMarshal.CreateSpan(ref ent.GetRelationData<RelMM>(ent2), 3);
//            Assert.AreEqual(data.Length, relationData.Length);
//            for (int i = 0; i < relationData.Length; i++)
//            {
//                Assert.False(Unsafe.IsAddressGreaterThan(ref data[i], ref relationData[i]) || Unsafe.IsAddressLessThan(ref data[i], ref relationData[i]));
//                Assert.AreEqual(data[i].dataVal, relationData[i].dataVal);
//            }
//        }

//        [Test]
//        public void HasRelationDiscriminatedTest()
//        {
//            var ent = world.CreateEntityImmediate();
//            var ent2 = world.CreateEntityImmediate();
//            var ent3 = world.CreateEntityImmediate();
//            var ent4 = world.CreateEntityImmediate();
//            ent.AddRelationTarget(ent2, new RelDSS(1));
//            ent.AddRelationTarget(ent3, new RelDSS(2));
//            ent.AddRelationTarget(ent4, new RelDSS(3));

//            Assert.True(ent.HasRelation<RelDSS>(ent2));
//            Assert.True(ent.HasRelation<RelDSS>(ent3));
//            Assert.True(ent.HasRelation<RelDSS>(ent4));

//            ent.RemoveRelationTarget<RelDSS>(ent2);
//            Assert.False(ent.HasRelation<RelDSS>(ent2));
//            Assert.True(ent.HasRelation<RelDSS>(ent3));
//            Assert.True(ent.HasRelation<RelDSS>(ent4));
//            Assert.Throws<ArgumentException>(() => { ent.HasRelation<RelDSS>(); });
//        }
//    }
//}
