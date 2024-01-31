using System;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// AudioManager is a singleton class responsible for managing audio playback in the game.
/// It uses a pooling system to efficiently handle multiple audio sources, allowing for
/// the prioritization and dynamic playback of audio clips. AudioSourceInstances are used
/// to manage individual audio sources, with functionality to set the audio clip, volume,
/// and other properties, as well as to handle positioning and conditional freeing of the sources.
/// 
/// Usage:
/// - Call the Request method to play an audio clip with specified properties such as volume,
///   reverb, looping, and priority. The AudioManager will either use a free audio source from the pool
///   or replace a currently playing source with lower priority.
/// - The AudioManager supports updating the position of audio sources and freeing them based on
///   custom conditions, making it suitable for a variety of audio playback scenarios in the game.
///
/// Note:
/// - Ensure the maxInstances value is set according to the needs of the game and available resources.
/// - The class uses a simple priority system where the instance with lowest Priority gets replaced if
///   instances are full.
/// </summary>


public class AudioManager : MonoBehaviour
{
    public int maxInstances = 50; // Maximum number of audio instances to be handled

    class AudioSourceInstance
    {
        private AudioSource audioSource;
        public bool free; // Flag indicating if the AudioSourceInstance is available
        public int Priority { get; private set; }
        public Func<Vector3> TargetPositionGetter {get; private set;}  // Function to get the position for the audio source
        public Func<bool> FreeCondition {get; private set;}           // Function to determine when to free the audio source

        public AudioSourceInstance(AudioSource audioSource, bool free, Func<Vector3> targetPositionGetter)
        {
            this.audioSource = audioSource;
            this.free = free;
            TargetPositionGetter = targetPositionGetter;
        }

        public void UpdatePosition()
        {
            // Update audio source position if a position getter is provided
            if(TargetPositionGetter != null)
                audioSource.transform.position = TargetPositionGetter();
        }

        // Sets the audio clip and related properties for playback
        public void SetAudioSource(AudioClip clip, Func<Vector3> targetPositionGetter, Func<bool> freeCondition, 
            float volume = 1f, float reverb = 0f, bool loop = false, int priority = 0)
        {
            free = false;
            audioSource.clip = clip;
            audioSource.gameObject.SetActive(true);

            audioSource.volume = volume;
            audioSource.reverbZoneMix = reverb;
            audioSource.loop = loop;

            TargetPositionGetter = targetPositionGetter;
            FreeCondition = freeCondition;
            
            Priority = priority;
        }

        // Frees the audio source for reuse
        public void Free()
        {
            free = true;
            audioSource.Stop();
            audioSource.gameObject.SetActive(false);
        }
    }

    private List<AudioSourceInstance> instances; // Pool of audio source instances

    private void Awake()
    {
        // Initialize the pool of audio source instances
        instances = new List<AudioSourceInstance>();
        for (int i = 0; i < maxInstances; i++)
        {
            GameObject thisObj = new GameObject("AudioSourceObject", typeof(AudioSource));
            thisObj.transform.SetParent(transform, false);
            AudioSource thisAudioSource = thisObj.GetComponent<AudioSource>();
            thisObj.SetActive(false);

            instances.Add(new AudioSourceInstance(thisAudioSource, true, () => Vector3.zero));
        }
    }

    void Update()
    {
        // Iterate through all instances to update positions and free if conditions are met
        foreach (var instance in instances)
        {
            if(instance.free)
                continue;
                
            instance.UpdatePosition();
            if (instance.FreeCondition != null && instance.FreeCondition())
            {
                instance.Free();
            }
        }
    }

    public void Request(AudioClip clip, Func<Vector3> targetPositionGetter, Func<bool> freeCondition, float volume = 1f,
        float reverb = 0f, bool loop = false, int priority = 0)
    {
        int lowestPriority = int.MaxValue;
        AudioSourceInstance lowestPriorityInstance = null;
        foreach (var instance in instances)
        {
            if (instance.free)
            {
                instance.SetAudioSource(clip, targetPositionGetter, freeCondition, volume, reverb, loop, priority);
                return;
            }

            if (lowestPriority > instance.Priority)
            {
                lowestPriority = instance.Priority;
                lowestPriorityInstance = instance;
            }
        }

        if (lowestPriority < priority)
        {
            lowestPriorityInstance.SetAudioSource(clip, targetPositionGetter, freeCondition, volume, reverb, loop, priority);
            return;
        }
        
        // No free instances
        Debug.Log("Audio pool instances full. Request ignored.");
    }
}
