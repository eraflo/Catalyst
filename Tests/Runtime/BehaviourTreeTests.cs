using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Eraflo.Catalyst.BehaviourTree;
using BT = Eraflo.Catalyst.BehaviourTree.BehaviourTree;

namespace Eraflo.Catalyst.Tests.Runtime
{
    public class BehaviourTreeTests
    {
        #region Blackboard Tests
        
        [Test]
        public void Blackboard_SetAndGet_Works()
        {
            var blackboard = new Blackboard();
            
            blackboard.Set("intKey", 42);
            blackboard.Set("stringKey", "hello");
            blackboard.Set("boolKey", true);
            
            Assert.AreEqual(42, blackboard.Get<int>("intKey"));
            Assert.AreEqual("hello", blackboard.Get<string>("stringKey"));
            Assert.AreEqual(true, blackboard.Get<bool>("boolKey"));
        }
        
        [Test]
        public void Blackboard_TryGet_ReturnsFalseForMissingKey()
        {
            var blackboard = new Blackboard();
            
            bool found = blackboard.TryGet<int>("missing", out int value);
            
            Assert.IsFalse(found);
            Assert.AreEqual(default(int), value);
        }
        
        [Test]
        public void Blackboard_Contains_Works()
        {
            var blackboard = new Blackboard();
            blackboard.Set("exists", 1);
            
            Assert.IsTrue(blackboard.Contains("exists"));
            Assert.IsFalse(blackboard.Contains("missing"));
        }
        
        [Test]
        public void Blackboard_Remove_Works()
        {
            var blackboard = new Blackboard();
            blackboard.Set("key", 1);
            
            Assert.IsTrue(blackboard.Contains("key"));
            
            blackboard.Remove("key");
            
            Assert.IsFalse(blackboard.Contains("key"));
        }
        
        [Test]
        public void Blackboard_Clear_RemovesAllKeys()
        {
            var blackboard = new Blackboard();
            blackboard.Set("a", 1);
            blackboard.Set("b", 2);
            blackboard.Set("c", 3);
            
            blackboard.Clear();
            
            Assert.IsFalse(blackboard.Contains("a"));
            Assert.IsFalse(blackboard.Contains("b"));
            Assert.IsFalse(blackboard.Contains("c"));
        }
        
        [Test]
        public void Blackboard_Clone_CopiesData()
        {
            var original = new Blackboard();
            original.Set("key", 42);
            
            var clone = original.Clone();
            
            Assert.AreEqual(42, clone.Get<int>("key"));
            
            // Verify independence
            clone.Set("key", 100);
            Assert.AreEqual(42, original.Get<int>("key"));
            Assert.AreEqual(100, clone.Get<int>("key"));
        }
        
        #endregion
        
        #region Selector Tests
        
        [Test]
        public void Selector_ReturnsSuccess_OnFirstChildSuccess()
        {
            var selector = ScriptableObject.CreateInstance<Selector>();
            var successNode = ScriptableObject.CreateInstance<TestSuccessNode>();
            var failNode = ScriptableObject.CreateInstance<TestFailNode>();
            
            selector.Children.Add(failNode);
            selector.Children.Add(successNode);
            selector.Children.Add(failNode);
            
            var result = selector.Evaluate();
            
            Assert.AreEqual(NodeState.Success, result);
            
            // Cleanup
            Object.DestroyImmediate(selector);
            Object.DestroyImmediate(successNode);
            Object.DestroyImmediate(failNode);
        }
        
        [Test]
        public void Selector_ReturnsFailure_WhenAllChildrenFail()
        {
            var selector = ScriptableObject.CreateInstance<Selector>();
            var fail1 = ScriptableObject.CreateInstance<TestFailNode>();
            var fail2 = ScriptableObject.CreateInstance<TestFailNode>();
            
            selector.Children.Add(fail1);
            selector.Children.Add(fail2);
            
            var result = selector.Evaluate();
            
            Assert.AreEqual(NodeState.Failure, result);
            
            Object.DestroyImmediate(selector);
            Object.DestroyImmediate(fail1);
            Object.DestroyImmediate(fail2);
        }
        
        #endregion
        
        #region Sequence Tests
        
        [Test]
        public void Sequence_ReturnsFailure_OnFirstChildFailure()
        {
            var sequence = ScriptableObject.CreateInstance<Sequence>();
            var successNode = ScriptableObject.CreateInstance<TestSuccessNode>();
            var failNode = ScriptableObject.CreateInstance<TestFailNode>();
            
            sequence.Children.Add(successNode);
            sequence.Children.Add(failNode);
            sequence.Children.Add(successNode);
            
            var result = sequence.Evaluate();
            
            Assert.AreEqual(NodeState.Failure, result);
            
            Object.DestroyImmediate(sequence);
            Object.DestroyImmediate(successNode);
            Object.DestroyImmediate(failNode);
        }
        
        [Test]
        public void Sequence_ReturnsSuccess_WhenAllChildrenSucceed()
        {
            var sequence = ScriptableObject.CreateInstance<Sequence>();
            var success1 = ScriptableObject.CreateInstance<TestSuccessNode>();
            var success2 = ScriptableObject.CreateInstance<TestSuccessNode>();
            
            sequence.Children.Add(success1);
            sequence.Children.Add(success2);
            
            var result = sequence.Evaluate();
            
            Assert.AreEqual(NodeState.Success, result);
            
            Object.DestroyImmediate(sequence);
            Object.DestroyImmediate(success1);
            Object.DestroyImmediate(success2);
        }
        
        #endregion
        
        #region Inverter Tests
        
        [Test]
        public void Inverter_InvertsSuccess()
        {
            var inverter = ScriptableObject.CreateInstance<Inverter>();
            var successNode = ScriptableObject.CreateInstance<TestSuccessNode>();
            
            inverter.Child = successNode;
            
            var result = inverter.Evaluate();
            
            Assert.AreEqual(NodeState.Failure, result);
            
            Object.DestroyImmediate(inverter);
            Object.DestroyImmediate(successNode);
        }
        
        [Test]
        public void Inverter_InvertsFailure()
        {
            var inverter = ScriptableObject.CreateInstance<Inverter>();
            var failNode = ScriptableObject.CreateInstance<TestFailNode>();
            
            inverter.Child = failNode;
            
            var result = inverter.Evaluate();
            
            Assert.AreEqual(NodeState.Success, result);
            
            Object.DestroyImmediate(inverter);
            Object.DestroyImmediate(failNode);
        }
        
        #endregion
        
        #region Repeater Tests
        
        [Test]
        public void Repeater_RepeatsNTimes()
        {
            var repeater = ScriptableObject.CreateInstance<Repeater>();
            var counter = ScriptableObject.CreateInstance<TestCounterNode>();
            
            repeater.RepeatCount = 3;
            repeater.Child = counter;
            
            // Run until completion
            NodeState state;
            int maxIterations = 10;
            int iterations = 0;
            
            do
            {
                state = repeater.Evaluate();
                iterations++;
            } while (state == NodeState.Running && iterations < maxIterations);
            
            Assert.AreEqual(NodeState.Success, state);
            Assert.AreEqual(3, counter.ExecutionCount);
            
            Object.DestroyImmediate(repeater);
            Object.DestroyImmediate(counter);
        }
        
        #endregion
        
        #region Parallel Tests
        
        [Test]
        public void Parallel_ReturnsSuccess_WhenAllChildrenSucceed()
        {
            var parallel = ScriptableObject.CreateInstance<Parallel>();
            var success1 = ScriptableObject.CreateInstance<TestSuccessNode>();
            var success2 = ScriptableObject.CreateInstance<TestSuccessNode>();
            
            parallel.SuccessPolicy = Parallel.Policy.RequireAll;
            parallel.Children.Add(success1);
            parallel.Children.Add(success2);
            
            var result = parallel.Evaluate();
            
            Assert.AreEqual(NodeState.Success, result);
            
            Object.DestroyImmediate(parallel);
            Object.DestroyImmediate(success1);
            Object.DestroyImmediate(success2);
        }
        
        [Test]
        public void Parallel_ReturnsFailure_WhenOneChildFails_WithRequireOnePolicy()
        {
            var parallel = ScriptableObject.CreateInstance<Parallel>();
            var success = ScriptableObject.CreateInstance<TestSuccessNode>();
            var fail = ScriptableObject.CreateInstance<TestFailNode>();
            
            parallel.FailurePolicy = Parallel.Policy.RequireOne;
            parallel.Children.Add(success);
            parallel.Children.Add(fail);
            
            var result = parallel.Evaluate();
            
            Assert.AreEqual(NodeState.Failure, result);
            
            Object.DestroyImmediate(parallel);
            Object.DestroyImmediate(success);
            Object.DestroyImmediate(fail);
        }
        
        #endregion
        
        #region Succeeder Tests
        
        [Test]
        public void Succeeder_AlwaysReturnsSuccess_EvenWhenChildFails()
        {
            var succeeder = ScriptableObject.CreateInstance<Succeeder>();
            var failNode = ScriptableObject.CreateInstance<TestFailNode>();
            
            succeeder.Child = failNode;
            
            var result = succeeder.Evaluate();
            
            Assert.AreEqual(NodeState.Success, result);
            
            Object.DestroyImmediate(succeeder);
            Object.DestroyImmediate(failNode);
        }
        
        #endregion
        
        #region Log Tests
        
        [Test]
        public void Log_ReturnsSuccess()
        {
            var log = ScriptableObject.CreateInstance<BTLog>();
            log.Message = "Test message";
            
            var result = log.Evaluate();
            
            Assert.AreEqual(NodeState.Success, result);
            
            Object.DestroyImmediate(log);
        }
        
        #endregion
        
        #region SetBlackboardValue Tests
        
        [Test]
        public void SetBlackboardValue_SetsIntValue()
        {
            var tree = ScriptableObject.CreateInstance<BT>();
            tree.Blackboard = new Blackboard();
            
            var setNode = ScriptableObject.CreateInstance<SetBlackboardValue>();
            setNode.Key = "testInt";
            setNode.Type = SetBlackboardValue.ValueType.Int;
            setNode.IntValue = 42;
            setNode.Tree = tree;
            
            var result = setNode.Evaluate();
            
            Assert.AreEqual(NodeState.Success, result);
            Assert.AreEqual(42, tree.Blackboard.Get<int>("testInt"));
            
            Object.DestroyImmediate(setNode);
            Object.DestroyImmediate(tree);
        }
        
        [Test]
        public void SetBlackboardValue_SetsBoolValue()
        {
            var tree = ScriptableObject.CreateInstance<BT>();
            tree.Blackboard = new Blackboard();
            
            var setNode = ScriptableObject.CreateInstance<SetBlackboardValue>();
            setNode.Key = "testBool";
            setNode.Type = SetBlackboardValue.ValueType.Bool;
            setNode.BoolValue = true;
            setNode.Tree = tree;
            
            var result = setNode.Evaluate();
            
            Assert.AreEqual(NodeState.Success, result);
            Assert.AreEqual(true, tree.Blackboard.Get<bool>("testBool"));
            
            Object.DestroyImmediate(setNode);
            Object.DestroyImmediate(tree);
        }
        
        #endregion
        
        #region BehaviourTreeNodeAttribute Tests
        
        [Test]
        public void BehaviourTreeNodeAttribute_SetsDisplayName()
        {
            var attr = new BehaviourTreeNodeAttribute("My Node");
            
            Assert.AreEqual("My Node", attr.DisplayName);
        }
        
        [Test]
        public void BehaviourTreeNodeAttribute_SetsCategoryAndDisplayName()
        {
            var attr = new BehaviourTreeNodeAttribute("Actions/Combat", "Attack");
            
            Assert.AreEqual("Actions/Combat", attr.Category);
            Assert.AreEqual("Attack", attr.DisplayName);
        }
        
        [Test]
        public void BehaviourTreeNodeAttribute_CanBeAppliedToCustomNode()
        {
            var type = typeof(TestAttributeNode);
            var attr = System.Attribute.GetCustomAttribute(type, typeof(BehaviourTreeNodeAttribute)) 
                       as BehaviourTreeNodeAttribute;
            
            Assert.IsNotNull(attr);
            Assert.AreEqual("Test", attr.Category);
            Assert.AreEqual("Attributed Node", attr.DisplayName);
        }
        
        [BehaviourTreeNode("Test", "Attributed Node")]
        private class TestAttributeNode : ActionNode
        {
            protected override NodeState OnUpdate() => NodeState.Success;
        }
        
        #endregion
        
        #region BlackboardCondition Tests
        
        [Test]
        public void BlackboardCondition_ReturnsSuccess_WhenConditionMet()
        {
            var tree = ScriptableObject.CreateInstance<BT>();
            tree.Blackboard = new Blackboard();
            tree.Blackboard.Set("health", 50);
            
            var condition = ScriptableObject.CreateInstance<BlackboardCondition>();
            condition.Key = "health";
            condition.Type = BlackboardCondition.ValueType.Int;
            condition.CompareOperator = BlackboardCondition.Operator.GreaterThan;
            condition.IntValue = 25;
            condition.Tree = tree;
            
            var result = condition.Evaluate();
            
            Assert.AreEqual(NodeState.Success, result);
            
            Object.DestroyImmediate(condition);
            Object.DestroyImmediate(tree);
        }
        
        [Test]
        public void BlackboardCondition_ReturnsFailure_WhenConditionNotMet()
        {
            var tree = ScriptableObject.CreateInstance<BT>();
            tree.Blackboard = new Blackboard();
            tree.Blackboard.Set("health", 10);
            
            var condition = ScriptableObject.CreateInstance<BlackboardCondition>();
            condition.Key = "health";
            condition.Type = BlackboardCondition.ValueType.Int;
            condition.CompareOperator = BlackboardCondition.Operator.GreaterThan;
            condition.IntValue = 25;
            condition.Tree = tree;
            
            var result = condition.Evaluate();
            
            Assert.AreEqual(NodeState.Failure, result);
            
            Object.DestroyImmediate(condition);
            Object.DestroyImmediate(tree);
        }
        
        [Test]
        public void BlackboardCondition_Exists_ReturnsSuccess_WhenKeyExists()
        {
            var tree = ScriptableObject.CreateInstance<BT>();
            tree.Blackboard = new Blackboard();
            tree.Blackboard.Set("myKey", 1);
            
            var condition = ScriptableObject.CreateInstance<BlackboardCondition>();
            condition.Key = "myKey";
            condition.Type = BlackboardCondition.ValueType.Exists;
            condition.CompareOperator = BlackboardCondition.Operator.Equals;
            condition.Tree = tree;
            
            var result = condition.Evaluate();
            
            Assert.AreEqual(NodeState.Success, result);
            
            Object.DestroyImmediate(condition);
            Object.DestroyImmediate(tree);
        }
        
        #endregion
        
        #region Helper Test Nodes
        
        private class TestSuccessNode : ActionNode
        {
            protected override NodeState OnUpdate() => NodeState.Success;
        }
        
        private class TestFailNode : ActionNode
        {
            protected override NodeState OnUpdate() => NodeState.Failure;
        }
        
        private class TestCounterNode : ActionNode
        {
            public int ExecutionCount;
            
            protected override NodeState OnUpdate()
            {
                ExecutionCount++;
                return NodeState.Success;
            }
        }
        
        #endregion
    }
}
