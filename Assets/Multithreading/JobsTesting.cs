using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Jobs;

public class JobsTesting : MonoBehaviour
{
    private NativeArray<float> result;
    public NativeArray<NativeArray<float>> results;
    public NativeArray<JobHandle> handles;
    private JobHandle handle;
    
    public struct Dumb : IJob
    {
        public float a, b;
        public NativeArray<float> result;
        public void Execute()
        {
            result[0] = a + b;
        }
    }

    private void Start()
    {
        handles = new NativeArray<JobHandle>(24, Allocator.Persistent);
    }

    void Update()
    {
        results = new NativeArray<NativeArray<float>>(24, Allocator.Persistent);
        for (int i = 0; i < 24; i++)
        {
            results[i] = new NativeArray<float>(1, Allocator.TempJob);
            Dumb thisDumbJob = new Dumb
            {
                a = i,
                b = 10,
                result = results[i]
            };
            handles[i] = thisDumbJob.Schedule();
        }
    }

    private void LateUpdate()
    {
        // JobHandle.CompleteAll(handles);

        for (int i = 0; i < 24; i++)
        {
            handles[i].Complete();
            print(results[i][0]);
            results[i].Dispose();
        }
    }
}
