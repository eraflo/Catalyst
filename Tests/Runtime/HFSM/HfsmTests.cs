using NUnit.Framework;
using Eraflo.Catalyst.HFSM;
using UnityEngine;
using System.Collections.Generic;

namespace Eraflo.Catalyst.Tests.Runtime.HFSM
{
    public class HfsmTests
    {
        private StateMachine _fsm;
        private TestState _root;
        private TestState _child;

        [SetUp]
        public void Setup()
        {
            App.Shutdown(); // Ensure fresh state
            _fsm = new StateMachine();
            _root = new TestState("Root");
            _child = new TestState("Child");
            
            _root.AddChild(_child);
            _fsm.SetRootState(_root);
        }

        [Test]
        public void Test_InitialState()
        {
            _fsm.Start();
            Assert.AreEqual(_child, _fsm.ActiveState);
            Assert.AreEqual(2, _fsm.ActivePath.Count);
        }

        [Test]
        public void Test_SimpleTransition()
        {
            var otherState = new TestState("Other");
            _root.AddChild(otherState);
            
            bool trigger = false;
            _child.AddTransition(new Transition(otherState, () => trigger));
            
            _fsm.Start();
            _fsm.Update();
            Assert.AreEqual(_child, _fsm.ActiveState);
            
            trigger = true;
            _fsm.Update();
            Assert.AreEqual(otherState, _fsm.ActiveState);
        }

        [Test]
        public void Test_FindStateByPath()
        {
            var state = _fsm.FindStateByPath("Root/Child");
            Assert.AreEqual(_child, state);
            
            var root = _fsm.FindStateByPath("Root");
            Assert.AreEqual(_root, root);
        }

        private class TestState : StateBase
        {
            public int EnterCount;
            public TestState(string name) : base(name) { }
            public override void OnEnter() => EnterCount++;
        }
    }
}
