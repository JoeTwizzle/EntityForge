//using System;
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
//        ArchetypeDefinition def = ArchetypeBuilder.Create().TreeRelation<RelSS>().Inc<Component1>().Inc<Component2>().Inc<Component3>().End();

//        World world;

//        [SetUp]
//        public void Init()
//        {
//            world = new World();
//        }

//        [Test]
//        public void GetRelationDataSingleTest()
//        {
//            var ent = world.CreateEntity();
//            var ent2 = world.CreateEntity();
//            ent.AddRelationTarget(ent2, new RelSS(1337));

//            Assert.AreEqual(ent.GetTreeRelationData<RelSS>(ent2), new RelSS(1337));
//        }

//        [Test]
//        public void GetRelationTargetSingleTest()
//        {
//            var ent = world.CreateEntity();
//            var ent2 = world.CreateEntity();
//            ent.AddRelationTarget(ent2, new RelSS(1337));

//            Assert.AreEqual(ent.GetRelationTarget<RelSS>(), ent2);
//        }

//        [Test]
//        public void GetRelationTargetsMultiTest()
//        {
//            var ent = world.CreateEntity();
//            var ent2 = world.CreateEntity();
//            var ent3 = world.CreateEntity();
//            var ent4 = world.CreateEntity();
//            ent.AddRelationTarget(ent2, new RelSM(1337));
//            ent.AddRelationTarget<RelSM>(ent3);
//            ent.AddRelationTarget<RelSM>(ent4);
//            ReadOnlySpan<Entity> targets = ent.GetRelationTargets<RelSM>();
//            Span<Entity> EntitiesPool = stackalloc Entity[3] { ent2, ent3, ent4 };
//            Assert.AreEqual(EntitiesPool.Length, targets.Length);
//            for (int i = 0; i < targets.Length; i++)
//            {
//                Assert.AreEqual(EntitiesPool[i], targets[i]);
//            }
//        }

//        [Test]
//        public void GetRelationDataMultiTest()
//        {
//            var ent = world.CreateEntity();
//            var ent2 = world.CreateEntity();
//            var ent3 = world.CreateEntity();
//            var ent4 = world.CreateEntity();
//            ent.AddRelationTarget(ent2, new RelMM(1));
//            ent.AddRelationTarget<RelMM>(ent3, new RelMM(2));
//            ent.AddRelationTarget<RelMM>(ent4, new RelMM(3));
//            Span<RelMM> relationData = ent.GetTreeRelationData<RelMM>();
//            Span<RelMM> Data = MemoryMarshal.CreateSpan(ref ent.GetTreeRelationData<RelMM>(ent2), 3);
//            Assert.AreEqual(Data.Length, relationData.Length);
//            for (int i = 0; i < relationData.Length; i++)
//            {
//                Assert.False(Unsafe.IsAddressGreaterThan(ref Data[i], ref relationData[i]) || Unsafe.IsAddressLessThan(ref Data[i], ref relationData[i]));
//                Assert.AreEqual(Data[i].dataVal, relationData[i].dataVal);
//            }
//        }

//        [Test]
//        public void HasRelationDiscriminatedTest()
//        {
//            var ent = world.CreateEntity();
//            var ent2 = world.CreateEntity();
//            var ent3 = world.CreateEntity();
//            var ent4 = world.CreateEntity();
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
