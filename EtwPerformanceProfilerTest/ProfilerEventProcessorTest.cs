﻿using System.Collections.Generic;
using EtwPerformanceProfiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EtwPerformanceProfilerTest
{
    [TestClass]
    public class ProfilerEventProcessorTest
    {
        /// <summary>
        /// 
        /// foo();
        /// SQL QUERY
        /// 
        /// foo()
        ///     var1 += 1;
        /// </summary>
        [TestMethod]
        public void BuildAggregatedCallTreeSqlAfterFunctionTest()
        {
            List<ProfilerEvent> profilerEventList = new List<ProfilerEvent>
                {        
                    new ProfilerEvent
                    {
                        ObjectId = 1,
                        Type = EventType.Statement,
                        StatementName = "foo"
                    }, // 0

                    new ProfilerEvent
                    {
                        ObjectId = 1,
                        Type = EventType.StartMethod,
                        StatementName = "foo"
                    }, // 1

                    new ProfilerEvent
                    {
                        ObjectId = 1,
                        Type = EventType.Statement,
                        StatementName = "var1 += 1"
                    }, // 2

                    new ProfilerEvent
                    {
                        ObjectId = 1,
                        Type = EventType.StopMethod,
                        StatementName = "foo"
                    }, // 3

                    new ProfilerEvent
                    {
                        ObjectId = 0,
                        Type = EventType.StartMethod,
                        StatementName = "SQL"
                    }, // 4

                    new ProfilerEvent
                    {
                        ObjectId = 0,
                        Type = EventType.StopMethod,
                        StatementName = "SQL"
                    }, // 5
                };

            AggregatedEventNode aggregatedCallTree = BuildAggregatedCallTree(profilerEventList);

            AggregatedEventNode expected = new AggregatedEventNode();
            AggregatedEventNode currentNode = expected;

            currentNode = currentNode.PushEventIntoCallStack(profilerEventList[0]); // +foo
            currentNode = currentNode.PushEventIntoCallStack(profilerEventList[2]); // +var1 += 1
            currentNode = currentNode.PopEventFromCallStackAndCalculateDuration(0); // -var1 += 1
            currentNode = currentNode.PopEventFromCallStackAndCalculateDuration(0); // -foo
            currentNode = currentNode.PushEventIntoCallStack(profilerEventList[4]); // +SQL
            currentNode.PopEventFromCallStackAndCalculateDuration(0); // -SQL

            AssertAggregatedEventNode(expected, aggregatedCallTree);
        }
        /// <summary>
        /// 
        /// FOR i:= 1 TO 3 DO
        ///     foo();
        /// 
        /// foo()
        ///     SELECTLATESTVERSION;
        /// 
        ///     r.FINDFIRST;
        ///     var1 += 1;
        ///     SLEEP(1000);
        /// 
        ///     foo1;
        /// 
        /// foo1()
        ///     foo2;
        ///     var1 += 1;
        /// 
        /// foo2()
        ///     var1 += 1;
        ///     MESSAGE('Hi!');
        /// </summary>
        [TestMethod]
        public void BuildAggregatedCallTreeNestedFunctionsTest()
        {
            List<ProfilerEvent>  profilerEventList = new List<ProfilerEvent>
                {
                    new ProfilerEvent
                        {
                            ObjectId = 1,
                            Type = EventType.Statement,
                            StatementName = "FOR i:= 1"
                        }, // 0
                    new ProfilerEvent
                        {
                            ObjectId = 1,
                            Type = EventType.Statement,
                            StatementName = "3"
                        } // 1
                };

            AddForIteration(profilerEventList);
            AddForIteration(profilerEventList);
            AddForIteration(profilerEventList);

            AggregatedEventNode aggregatedCallTree = BuildAggregatedCallTree(profilerEventList);

            AggregatedEventNode expected = new AggregatedEventNode();
            AggregatedEventNode currentNode = expected;

            currentNode = currentNode.PushEventIntoCallStack(profilerEventList[0]); // +FOR i:= 1
            currentNode = currentNode.PopEventFromCallStackAndCalculateDuration(0); // -FOR i:= 1
            currentNode = currentNode.PushEventIntoCallStack(profilerEventList[1]); // +3
            currentNode = currentNode.PopEventFromCallStackAndCalculateDuration(0); // -3

            AddForIteration(currentNode, profilerEventList);
            AddForIteration(currentNode, profilerEventList);
            AddForIteration(currentNode, profilerEventList);

            AssertAggregatedEventNode(expected, aggregatedCallTree);
        }

        private static void AddForIteration(AggregatedEventNode currentNode, List<ProfilerEvent> profilerEventList)
        {
            currentNode = currentNode.PushEventIntoCallStack(profilerEventList[2]); // +foo
            currentNode = currentNode.PushEventIntoCallStack(profilerEventList[4]); // +SELECTLATESTVERSION
            currentNode = currentNode.PopEventFromCallStackAndCalculateDuration(0); // -SELECTLATESTVERSION
            currentNode = currentNode.PushEventIntoCallStack(profilerEventList[5]); // +r.FINDFIRST
            currentNode = currentNode.PushEventIntoCallStack(profilerEventList[6]); // +SELECT
            currentNode = currentNode.PopEventFromCallStackAndCalculateDuration(0); // -SELECT
            currentNode = currentNode.PopEventFromCallStackAndCalculateDuration(0); // -r.FINDFIRST
            currentNode = currentNode.PushEventIntoCallStack(profilerEventList[8]); // +var1 += 1
            currentNode = currentNode.PopEventFromCallStackAndCalculateDuration(0); // -var1 += 1
            currentNode = currentNode.PushEventIntoCallStack(profilerEventList[9]); // +SLEEP(1000)
            currentNode = currentNode.PopEventFromCallStackAndCalculateDuration(0); // -SLEEP(1000)
            currentNode = currentNode.PushEventIntoCallStack(profilerEventList[10]); // +foo1
            currentNode = currentNode.PushEventIntoCallStack(profilerEventList[12]); // +foo2 
            currentNode = currentNode.PushEventIntoCallStack(profilerEventList[14]); // +var1 += 1
            currentNode = currentNode.PopEventFromCallStackAndCalculateDuration(0); // -var1 += 1 
            currentNode = currentNode.PushEventIntoCallStack(profilerEventList[15]); // +MESSAGE('Hi!')
            currentNode = currentNode.PopEventFromCallStackAndCalculateDuration(0); // -MESSAGE('Hi!')
            currentNode = currentNode.PopEventFromCallStackAndCalculateDuration(0); // -foo2
            currentNode = currentNode.PushEventIntoCallStack(profilerEventList[17]); // +var1 += 1
            currentNode = currentNode.PopEventFromCallStackAndCalculateDuration(0); // -var1 += 1
            currentNode = currentNode.PopEventFromCallStackAndCalculateDuration(0); // -foo1
            currentNode.PopEventFromCallStackAndCalculateDuration(0); // -foo
        }

        private void AddForIteration(List<ProfilerEvent> profilerEventList)
        {
            profilerEventList.Add(
                new ProfilerEvent
                {
                    ObjectId = 1,
                    Type = EventType.Statement,
                    StatementName = "foo"
                }); // 2

            profilerEventList.Add(
                new ProfilerEvent
                {
                    ObjectId = 1,
                    Type = EventType.StartMethod,
                    StatementName = "foo"
                }); // 3

            profilerEventList.Add(
                new ProfilerEvent
                {
                    ObjectId = 1,
                    Type = EventType.Statement,
                    StatementName = "SELECTLATESTVERSION"
                }); // 4

            profilerEventList.Add(
                new ProfilerEvent
                {
                    ObjectId = 1,
                    Type = EventType.Statement,
                    StatementName = "r.FINDFIRST"
                }); // 5

            profilerEventList.Add(
                new ProfilerEvent
                {
                    ObjectId = 0,
                    Type = EventType.StartMethod,
                    StatementName = "SELECT"
                }); // 6

            profilerEventList.Add(
                new ProfilerEvent
                {
                    ObjectId = 0,
                    Type = EventType.StopMethod,
                    StatementName = "SELECT"
                }); // 7

            profilerEventList.Add(
                new ProfilerEvent
                {
                    ObjectId = 1,
                    Type = EventType.Statement,
                    StatementName = "var1 += 1"
                }); // 8

            profilerEventList.Add(
                new ProfilerEvent
                {
                    ObjectId = 1,
                    Type = EventType.Statement,
                    StatementName = "SLEEP(1000)"
                }); // 9

            profilerEventList.Add(
                new ProfilerEvent
                {
                    ObjectId = 1,
                    Type = EventType.Statement,
                    StatementName = "foo1"
                }); // 10

            profilerEventList.Add(
                new ProfilerEvent
                {
                    ObjectId = 1,
                    Type = EventType.StartMethod,
                    StatementName = "foo1"
                }); // 11

            profilerEventList.Add(
                new ProfilerEvent
                {
                    ObjectId = 1,
                    Type = EventType.Statement,
                    StatementName = "foo2"
                }); // 12

            profilerEventList.Add(
                new ProfilerEvent
                {
                    ObjectId = 1,
                    Type = EventType.StartMethod,
                    StatementName = "foo2"
                }); // 13

            profilerEventList.Add(
                new ProfilerEvent
                {
                    ObjectId = 1,
                    Type = EventType.Statement,
                    StatementName = "var1 += 1"
                }); // 14

            profilerEventList.Add(
                new ProfilerEvent
                {
                    ObjectId = 1,
                    Type = EventType.Statement,
                    StatementName = "MESSAGE('Hi!')"
                }); // 15

            profilerEventList.Add(
                new ProfilerEvent
                {
                    ObjectId = 1,
                    Type = EventType.StopMethod,
                    StatementName = "foo2"
                }); // 16

            profilerEventList.Add(
                new ProfilerEvent
                {
                    ObjectId = 1,
                    Type = EventType.Statement,
                    StatementName = "var1 += 1"
                }); // 17

            profilerEventList.Add(
                new ProfilerEvent
                {
                    ObjectId = 1,
                    Type = EventType.StopMethod,
                    StatementName = "foo1"
                }); // 18

            profilerEventList.Add(
                new ProfilerEvent
                {
                    ObjectId = 1,
                    Type = EventType.StopMethod,
                    StatementName = "foo"
                }); // 19
        }

        /// <summary>
        ///IF predicate1 OR predicate2 THEN
        /// i := 0;
        /// 
        /// predicate1() res : Boolean
        ///     p1 += 1;
        /// 
        ///     EXIT(FALSE);
        /// 
        /// predicate2() rec : Boolean
        ///     p2 += 1;
        /// 
        ///     EXIT(TRUE);
        /// </summary>
        [TestMethod]
        public void BuildAggregatedCallTreeIfWithOrTest()
        {
            List<ProfilerEvent> profilerEventList = new List<ProfilerEvent>
                {
                    new ProfilerEvent
                        {
                            ObjectId = 1,
                            Type = EventType.Statement,
                            StatementName = "IF predicate1 OR predicate2"
                        }, // 0
                    new ProfilerEvent
                        {
                            ObjectId = 1,
                            Type = EventType.StartMethod,
                            StatementName = "predicate1"
                        }, // 1
                    new ProfilerEvent
                        {
                            ObjectId = 1,
                            Type = EventType.Statement,
                            StatementName = "p1 += 1"
                        }, // 2
                    new ProfilerEvent
                        {
                            ObjectId = 1,
                            Type = EventType.Statement,
                            StatementName = "EXIT(FALSE)"
                        }, // 3
                    new ProfilerEvent
                        {
                            ObjectId = 1,
                            Type = EventType.StopMethod,
                            StatementName = "predicate1"
                        }, // 4
                    new ProfilerEvent
                        {
                            ObjectId = 1,
                            Type = EventType.StartMethod,
                            StatementName = "predicate2"
                        }, // 5
                    new ProfilerEvent
                        {
                            ObjectId = 1,
                            Type = EventType.Statement,
                            StatementName = "p2 += 1"
                        }, // 6
                    new ProfilerEvent
                        {
                            ObjectId = 1,
                            Type = EventType.Statement,
                            StatementName = "EXIT(TRUE)"
                        }, // 7
                    new ProfilerEvent
                        {
                            ObjectId = 1,
                            Type = EventType.StopMethod,
                            StatementName = "predicate2"
                        }, // 8
                    new ProfilerEvent
                        {
                            ObjectId = 1,
                            Type = EventType.Statement,
                            StatementName = "i := 0"
                        }, // 9
                };

            AggregatedEventNode aggregatedCallTree = BuildAggregatedCallTree(profilerEventList);

            AggregatedEventNode expected = new AggregatedEventNode();
            AggregatedEventNode currentNode = expected;

            currentNode = currentNode.PushEventIntoCallStack(profilerEventList[0]); // +IF predicate1 OR predicate2
            currentNode = currentNode.PushEventIntoCallStack(profilerEventList[2]); // +p1 += 1
            currentNode = currentNode.PopEventFromCallStackAndCalculateDuration(0); // -p1 += 1
            currentNode = currentNode.PushEventIntoCallStack(profilerEventList[3]); // +EXIT(FALSE)
            currentNode = currentNode.PopEventFromCallStackAndCalculateDuration(0); // -EXIT(FALSE)
            currentNode = currentNode.PushEventIntoCallStack(profilerEventList[6]); // +p2 += 1
            currentNode = currentNode.PopEventFromCallStackAndCalculateDuration(0); // -p2 += 1
            currentNode = currentNode.PushEventIntoCallStack(profilerEventList[7]); // +EXIT(TRUE)
            currentNode = currentNode.PopEventFromCallStackAndCalculateDuration(0); // -EXIT(TRUE)
            currentNode = currentNode.PopEventFromCallStackAndCalculateDuration(0); // -IF predicate1 OR predicate2
            currentNode.PushEventIntoCallStack(profilerEventList[9]); // +i := 0

            AssertAggregatedEventNode(expected, aggregatedCallTree);
        }

        /// <summary>
        /// "Clear Codeunit 1 calls";
        /// Stop
        /// 
        /// Clear Codeunit 1 calls - OnAction()
        ///     codeUnit1Call := FALSE;
        ///
        ///     EXIT;
        /// 
        /// Stop - OnAction()
        ///     ProfilerStarted := FALSE;
        /// 
        ///     SLEEP(5000);
        /// </summary>
        [TestMethod]
        public void BuildAggregatedCallTreeTwoRootMethodsTest()
        {
            List<ProfilerEvent> profilerEventList = new List<ProfilerEvent>
                {
                    new ProfilerEvent
                        {
                            ObjectId = 1,
                            Type = EventType.StartMethod,
                            StatementName = "Clear Codeunit 1 calls - OnAction"
                        }, // 0
                    new ProfilerEvent
                        {
                            ObjectId = 1,
                            Type = EventType.Statement,
                            StatementName = "codeUnit1Call := FALSE"
                        }, // 1
                    new ProfilerEvent
                        {
                            ObjectId = 1,
                            Type = EventType.Statement,
                            StatementName = "EXIT"
                        }, // 2
                    new ProfilerEvent
                        {
                            ObjectId = 1,
                            Type = EventType.StopMethod,
                            StatementName = "Clear Codeunit 1 calls - OnAction"
                        }, // 3
                    new ProfilerEvent
                        {
                            ObjectId = 1,
                            Type = EventType.StartMethod,
                            StatementName = "Stop - OnAction"
                        }, // 4
                    new ProfilerEvent
                        {
                            ObjectId = 1,
                            Type = EventType.Statement,
                            StatementName = "ProfilerStarted := FALSE"
                        }, // 5
                    new ProfilerEvent
                        {
                            ObjectId = 1,
                            Type = EventType.Statement,
                            StatementName = "SLEEP(5000)"
                        }, // 6
                };

            AggregatedEventNode aggregatedCallTree = BuildAggregatedCallTree(profilerEventList);

            AggregatedEventNode expected = new AggregatedEventNode();
            AggregatedEventNode currentNode = expected;

            currentNode = currentNode.PushEventIntoCallStack(profilerEventList[0]); // +Clear Codeunit 1 calls - OnAction
            currentNode = currentNode.PushEventIntoCallStack(profilerEventList[1]); // +codeUnit1Call := FALSE
            currentNode = currentNode.PopEventFromCallStackAndCalculateDuration(0); // -codeUnit1Call := FALSE
            currentNode = currentNode.PushEventIntoCallStack(profilerEventList[2]); // +EXIT
            currentNode = currentNode.PopEventFromCallStackAndCalculateDuration(0); // -EXIT
            currentNode = currentNode.PopEventFromCallStackAndCalculateDuration(0); // -Clear Codeunit 1 calls - OnAction
            currentNode = currentNode.PushEventIntoCallStack(profilerEventList[4]); // +Stop - OnAction
            currentNode = currentNode.PushEventIntoCallStack(profilerEventList[5]); // +ProfilerStarted := FALSE
            currentNode = currentNode.PopEventFromCallStackAndCalculateDuration(0); // -ProfilerStarted := FALSE
            currentNode.PushEventIntoCallStack(profilerEventList[6]); // +SLEEP(5000)

            AssertAggregatedEventNode(expected, aggregatedCallTree);
        }

        private void AssertAggregatedEventNode(AggregatedEventNode expected, AggregatedEventNode aggregatedCallTree)
        {
            Assert.AreEqual(expected.StatementName, aggregatedCallTree.StatementName);
            Assert.AreEqual(expected.HitCount, aggregatedCallTree.HitCount);
            Assert.AreEqual(expected.Children.Count, aggregatedCallTree.Children.Count);

            for (int i = 0; i < expected.Children.Count; ++i)
            {
                AssertAggregatedEventNode(expected.Children[i], aggregatedCallTree.Children[i]);
            }
        }

        [TestMethod]
        public void GetStatementFromTheCacheTest()
        {
            var profilerEventProcessor = new ProfilerEventAggregator(0);

            const string Statement = "statement";
            profilerEventProcessor.GetStatementFromTheCache(Statement);

            for (int i = 0; i < 10; ++i)
            {
                string newStatement = new string(new [] {'s', 't', 'a', 't', 'e', 'm', 'e', 'n', 't'});
                string cahcedStatement = profilerEventProcessor.GetStatementFromTheCache(newStatement);

                Assert.IsTrue(object.ReferenceEquals(Statement, cahcedStatement));
            }
        }

        /// <summary>
        /// Builds the accumulated result of processing the stored ETW events
        /// </summary>
        /// <param name="profilerEvents">The list of profiler events.</param>
        /// <returns>An instance of an AggregatedEventNode tree.</returns>
        internal static AggregatedEventNode BuildAggregatedCallTree(IList<ProfilerEvent> profilerEvents)
        {
            AggregatedEventNode aggregatedCallTree = new AggregatedEventNode();
            AggregatedEventNode currentAggregatedEventNode = aggregatedCallTree;

            ProfilerEvent? previousProfilerEvent = null;
            ProfilerEvent? currentProfilerEvent = null;

            for (int i = 0; i < profilerEvents.Count; i++)
            {
                currentProfilerEvent = profilerEvents[i];

                ProfilerEventAggregator.AddProfilerEventToAggregatedCallTree(previousProfilerEvent, currentProfilerEvent, ref currentAggregatedEventNode);

                previousProfilerEvent = currentProfilerEvent;
            }

            ProfilerEventAggregator.AddProfilerEventToAggregatedCallTree(previousProfilerEvent, null, ref currentAggregatedEventNode);

            return aggregatedCallTree;
        }

        /// <summary>
        /// 
        /// RootMethod; // 1
        /// RootMethod; // 2
        /// 
        /// RootMethod()
        ///     rec1.DELETEALL; 
        ///     // first time it calls foo();
        ///     rec2.DELETEALL;
        /// 
        /// foo()
        ///     var1 += 1;
        /// 
        /// Same statement called twice. First time it issues method call second time does not.
        /// </summary>
        [TestMethod]
        public void BuildAggregatedCallTreeSameStatementCalledTwice()
        {
            List<ProfilerEvent> profilerEventList = new List<ProfilerEvent>
                {        
                    new ProfilerEvent
                    {
                        ObjectId = 1,
                        Type = EventType.StartMethod,
                        StatementName = "RootMethod"
                    }, // 0

                    new ProfilerEvent
                    {
                        ObjectId = 1,
                        Type = EventType.Statement,
                        StatementName = "rec1.DELETEALL"
                    }, // 1

                    new ProfilerEvent
                    {
                        ObjectId = 1,
                        Type = EventType.StartMethod,
                        StatementName = "foo"
                    }, // 2

                    new ProfilerEvent
                    {
                        ObjectId = 1,
                        Type = EventType.Statement,
                        StatementName = "var1 += 1"
                    }, // 3

                    new ProfilerEvent
                    {
                        ObjectId = 1,
                        Type = EventType.StopMethod,
                        StatementName = "foo"
                    }, // 4

                    new ProfilerEvent
                    {
                        ObjectId = 1,
                        Type = EventType.Statement,
                        StatementName = "rec2.DELETEALL"
                    }, // 5

                    new ProfilerEvent
                    {
                        ObjectId = 1,
                        Type = EventType.StopMethod,
                        StatementName = "RootMethod"
                    }, // 6

                    new ProfilerEvent
                    {
                        ObjectId = 1,
                        Type = EventType.StartMethod,
                        StatementName = "RootMethod"
                    }, // 7

                    new ProfilerEvent
                    {
                        ObjectId = 1,
                        Type = EventType.Statement,
                        StatementName = "rec1.DELETEALL"
                    }, // 8

                    new ProfilerEvent
                    {
                        ObjectId = 1,
                        Type = EventType.Statement,
                        StatementName = "rec2.DELETEALL"
                    }, // 9

                    new ProfilerEvent
                    {
                        ObjectId = 1,
                        Type = EventType.StopMethod,
                        StatementName = "RootMethod"
                    }, // 10
                };

            AggregatedEventNode aggregatedCallTree = BuildAggregatedCallTree(profilerEventList);

            AggregatedEventNode expected = new AggregatedEventNode();
            AggregatedEventNode currentNode = expected;

            currentNode = currentNode.PushEventIntoCallStack(profilerEventList[0]); // +RootMethod
            currentNode = currentNode.PushEventIntoCallStack(profilerEventList[1]); // +rec1.DELETEALL
            currentNode = currentNode.PushEventIntoCallStack(profilerEventList[3]); // +var1 += 1
            currentNode = currentNode.PopEventFromCallStackAndCalculateDuration(0); // -var1 += 1
            currentNode = currentNode.PopEventFromCallStackAndCalculateDuration(0); // -rec1.DELETEALL
            currentNode = currentNode.PushEventIntoCallStack(profilerEventList[5]); // +rec2.DELETEALL
            currentNode = currentNode.PopEventFromCallStackAndCalculateDuration(0); // -rec2.DELETEALL
            currentNode = currentNode.PopEventFromCallStackAndCalculateDuration(0); // -RootMethod
            currentNode = currentNode.PushEventIntoCallStack(profilerEventList[0]); // +RootMethod
            currentNode = currentNode.PushEventIntoCallStack(profilerEventList[1]); // +rec1.DELETEALL
            currentNode = currentNode.PopEventFromCallStackAndCalculateDuration(0); // -rec1.DELETEALL
            currentNode = currentNode.PushEventIntoCallStack(profilerEventList[5]); // +rec2.DELETEALL
            currentNode = currentNode.PopEventFromCallStackAndCalculateDuration(0); // -rec2.DELETEALL
            currentNode = currentNode.PopEventFromCallStackAndCalculateDuration(0); // -RootMethod

            AssertAggregatedEventNode(expected, aggregatedCallTree);
        }
    }
}
