﻿//--------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
//--------------------------------------------------------------------------

using System.Diagnostics;
using System.Collections.Generic;

namespace EtwPerformanceProfiler
{
    /// <summary>
    /// Represents the aggregated call tree.
    /// </summary>
    internal class AggregatedEventNode
    {
        /// <summary>
        /// Gets or sets the type of the object.
        /// </summary>
        internal string ObjectType { get; private set; }

        /// <summary>
        /// Gets or sets the object id.
        /// </summary>
        internal int ObjectId { get; private set; }

        /// <summary>
        /// Gets or sets the line no.
        /// </summary>
        internal int LineNo { get; private set; }

        /// <summary>
        /// Gets or sets the name of the Statement.
        /// </summary>
        internal string StatementName { get; set; }

        /// <summary>
        /// Gets or sets the duration in 100ns.
        /// </summary>
        internal double DurationMSec { get; private set; }

        /// <summary>
        /// Gets the children of the current node.
        /// </summary>
        internal List<AggregatedEventNode> Children { get; private set; }

        /// <summary>
        /// Gets or sets the number times we executed this statement.
        /// </summary>
        internal int HitCount { get; private set; }

        /// <summary>
        /// Gets or sets the time stamp in 100ns.
        /// </summary>
        internal double TimeStampRelativeMSec { get; private set; }

        /// <summary>
        /// Gets or sets the parent node.
        /// </summary>
        internal AggregatedEventNode Parent { get; private set; }

        /// <summary>
        /// Gets or sets the original type of the node.
        /// </summary>
        internal EventType OriginalType { get; private set; }

        /// <summary>
        /// Gets or sets the evaluated type. The statement type can change to a start type 
        /// during aggregation if the statement is a function call.
        /// </summary>
        internal EventType EvaluatedType { get; set; }

        /// <summary>
        /// Depth of the current element in the tree.
        /// </summary>
        internal int Depth { get; set; }

        /// <summary>
        /// Returns true if this is the none AL event.
        /// </summary>
        internal bool IsNoneAlEvent
        {
            get { return !this.IsAlEvent; }
        }

        /// <summary>
        /// Returns true if this is the AL event.
        /// </summary>
        internal bool IsAlEvent
        {
            get { return this.ObjectId != 0; }
        }
             
        /// <summary>
        /// Initializes a new instance of the <see cref="AggregatedEventNode"/> class.
        /// </summary>
        /// <param name="parent">Parent <see cref="AggregatedEventNode"/>.</param>
        internal AggregatedEventNode(AggregatedEventNode parent = null)
        {
            this.Children = new List<AggregatedEventNode>();
            this.Parent = parent;
            this.Depth = parent != null ? parent.Depth + 1 : 0;
        }

        /// <summary>
        /// Pushes event into call stack. It might get or create aggregated event.
        /// </summary>
        /// <param name="profilerEvent">The profiler event.</param>
        /// <returns>The aggregated event node.</returns>
        internal AggregatedEventNode PushEventIntoCallStack(ProfilerEvent profilerEvent)
        {
            Debug.Assert(profilerEvent.Type == EventType.Statement || profilerEvent.Type == EventType.StartMethod);

            AggregatedEventNode res = this.Children.Find(e => 
                e.ObjectType == profilerEvent.ObjectType &&
                e.ObjectId == profilerEvent.ObjectId &&
                e.LineNo == profilerEvent.LineNo &&
                e.StatementName == profilerEvent.StatementName);

            if (res != null)
            {
                // We need to initialize state of the AggregatedEventNode.
                // Otherwise duration will not be calculated correctly or we can get broken tree structure.
                res.EvaluatedType = profilerEvent.Type;
                res.TimeStampRelativeMSec = profilerEvent.TimeStampRelativeMSec;

                ++res.HitCount;
                return res;
            }

            res = new AggregatedEventNode(this)
                {
                    ObjectType = profilerEvent.ObjectType,
                    ObjectId = profilerEvent.ObjectId,
                    LineNo = profilerEvent.LineNo,
                    StatementName = profilerEvent.StatementName,
                    TimeStampRelativeMSec = profilerEvent.TimeStampRelativeMSec,
                    OriginalType = profilerEvent.Type,
                    EvaluatedType = profilerEvent.Type
                };

            this.Children.Add(res);

            ++res.HitCount;
            return res;
        }

        internal AggregatedEventNode PopEventFromCallStackAndCalculateDuration(double endTimeStampRelativeMSec)
        {
            this.DurationMSec += (endTimeStampRelativeMSec - this.TimeStampRelativeMSec);

            return this.Parent;
        }
    }
}