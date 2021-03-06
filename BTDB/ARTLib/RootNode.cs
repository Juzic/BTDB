﻿using System;
using System.Threading;

namespace BTDB.ARTLib
{
    internal class RootNode : IRootNode
    {
        internal RootNode(ARTImpl impl)
        {
            _impl = impl;
            _root = IntPtr.Zero;
            _writtable = true;
            _referenceCount = 1;
        }

        int _referenceCount;
        internal IntPtr _root;
        internal ARTImpl _impl;
        internal bool _writtable;

        public ulong CommitUlong { get; set; }
        public long TransactionId { get; set; }
        public string DescriptionForLeaks { get; set; }

        public void Dispose()
        {
            _impl.Dereference(_root);
        }

        public IRootNode Snapshot()
        {
            var snapshot = new RootNode(_impl);
            snapshot._writtable = false;
            snapshot._root = _root;
            snapshot.CommitUlong = CommitUlong;
            snapshot.TransactionId = TransactionId;
            snapshot._ulongs = _ulongs == null ? null : (ulong[])_ulongs.Clone();
            if (_writtable)
                TransactionId++;
            NodeUtils.Reference(_root);
            return snapshot;
        }

        public ICursor CreateCursor()
        {
            return new Cursor(this);
        }

        public long GetCount()
        {
            if (_root == IntPtr.Zero) return 0;
            ref var header = ref NodeUtils.Ptr2NodeHeader(_root);
            return (long)header._recursiveChildCount;
        }

        public void RevertTo(IRootNode snapshot)
        {
            if (!_writtable)
                throw new InvalidOperationException("Only writtable root node could be reverted");
            var oldRoot = _root;
            _root = ((RootNode)snapshot)._root;
            _ulongs = ((RootNode)snapshot)._ulongs == null ? null : (ulong[])((RootNode)snapshot)._ulongs.Clone();
            CommitUlong = ((RootNode)snapshot).CommitUlong;
            TransactionId = ((RootNode)snapshot).TransactionId;
            if (oldRoot != _root)
            {
                NodeUtils.Reference(_root);
                _impl.Dereference(oldRoot);
            }
        }

        ulong[] _ulongs;

        public ulong GetUlong(uint idx)
        {
            if (_ulongs == null) return 0;
            if (idx >= _ulongs.Length) return 0;
            return _ulongs[idx];
        }

        public void SetUlong(uint idx, ulong value)
        {
            if (_ulongs == null || idx >= _ulongs.Length)
                Array.Resize(ref _ulongs, (int)(idx + 1));
            _ulongs[idx] = value;
        }

        public uint GetUlongCount()
        {
            return _ulongs == null ? 0U : (uint)_ulongs.Length;
        }

        public void Reference()
        {
            Interlocked.Increment(ref _referenceCount);
        }

        public bool Dereference()
        {
            return Interlocked.Decrement(ref _referenceCount) == 0;
        }
    }
}
