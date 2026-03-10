using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Analytics;
using Unity.Services.Analytics;
using Unity.Services.Core;

namespace Game.Systems
{
    /// <summary>
    /// Service interface for Unity Analytics event tracking and analytics.
    /// Provides methods for tracking custom events, errors, and user properties.
    /// </summary>
    public interface IAnalyticsService
    {
        /// <summary>
        /// Whether Analytics is initialized and ready to use.
        /// </summary>
        bool IsInitialized { get; }
        
        /// <summary>
        /// Initializes the Analytics service.
        /// </summary>
        UniTask InitializeAsync();
        
        /// <summary>
        /// Tracks a custom event with parameters.
        /// </summary>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="parameters">Event parameters (key-value pairs).</param>
        void TrackEvent(string eventName, Dictionary<string, object> parameters = null);
        
        /// <summary>
        /// Reports an exception/error to Analytics.
        /// </summary>
        /// <param name="exception">The exception to report.</param>
        /// <param name="context">Additional context information.</param>
        void ReportException(Exception exception, Dictionary<string, object> context = null);
        
        /// <summary>
        /// Reports a custom error message to Analytics.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="context">Additional context information.</param>
        void ReportError(string message, Dictionary<string, object> context = null);
        
        /// <summary>
        /// Sets user properties for analytics.
        /// </summary>
        /// <param name="playerId">The player ID.</param>
        /// <param name="properties">Additional user properties.</param>
        void SetUserProperties(string playerId, Dictionary<string, object> properties = null);
        
        /// <summary>
        /// Tracks a screen view/page view.
        /// </summary>
        /// <param name="screenName">The name of the screen/view.</param>
        /// <param name="parameters">Additional parameters.</param>
        void TrackScreenView(string screenName, Dictionary<string, object> parameters = null);
        
        /// <summary>
        /// Tracks a purchase transaction.
        /// </summary>
        /// <param name="productId">The product ID.</param>
        /// <param name="amount">The purchase amount.</param>
        /// <param name="currency">The currency code (e.g., "USD").</param>
        /// <param name="parameters">Additional parameters.</param>
        void TrackPurchase(string productId, decimal amount, string currency, Dictionary<string, object> parameters = null);
        
        /// <summary>
        /// Tracks a level/progression event.
        /// </summary>
        /// <param name="levelName">The level or progression name.</param>
        /// <param name="levelIndex">The level index/number.</param>
        /// <param name="parameters">Additional parameters.</param>
        void TrackLevel(string levelName, int levelIndex, Dictionary<string, object> parameters = null);
        
        /// <summary>
        /// Shuts down the Analytics service.
        /// </summary>
        UniTask ShutdownAsync();
    }
    
    /// <summary>
    /// Production-ready implementation of Unity Analytics service for event tracking and analytics.
    /// Handles initialization, event tracking, error reporting, user properties, and proper error handling.
    /// </summary>
    public class AnalyticsServices : IAnalyticsService
    {
        private bool _isInitialized = false;
        private string _playerId;
        private readonly Dictionary<string, object> _userProperties = new Dictionary<string, object>();
        
        public bool IsInitialized => _isInitialized;
        
        /// <summary>
        /// Initializes the Analytics service.
        /// </summary>
        public async UniTask InitializeAsync()
        {
            if (_isInitialized)
            {
                Debug.Log("[Analytics] Already initialized.");
                return;
            }
            
            try
            {
                Debug.Log("[Analytics] Initializing Unity Analytics service...");
                
                // Unity Analytics is automatically initialized when Unity Services are initialized
                // We just need to verify it's available
                if (Application.isEditor && !Application.isPlaying)
                {
                    Debug.LogWarning("[Analytics] Analytics may not work properly in edit mode.");
                }
                
                // Set initial user properties
                SetInitialUserProperties();
                
                _isInitialized = true;
                Debug.Log("[Analytics] Unity Analytics service initialized successfully.");
                
                // Track initialization event
                TrackEvent("analytics_initialized", new Dictionary<string, object>
                {
                    { "environment", GetEnvironment() },
                    { "unity_version", Application.unityVersion },
                    { "platform", Application.platform.ToString() }
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[Analytics] Failed to initialize Analytics service: {e.Message}");
                // Don't throw - allow game to continue even if Analytics fails
            }
            
            await UniTask.CompletedTask;
        }
        
        /// <summary>
        /// Sets initial user properties based on device and application info.
        /// </summary>
        private void SetInitialUserProperties()
        {
            try
            {
                _userProperties.Clear();
                _userProperties["device_model"] = SystemInfo.deviceModel;
                _userProperties["device_type"] = SystemInfo.deviceType.ToString();
                _userProperties["operating_system"] = SystemInfo.operatingSystem;
                _userProperties["processor_type"] = SystemInfo.processorType;
                _userProperties["processor_count"] = SystemInfo.processorCount;
                _userProperties["system_memory"] = SystemInfo.systemMemorySize;
                _userProperties["graphics_device"] = SystemInfo.graphicsDeviceName;
                _userProperties["graphics_memory"] = SystemInfo.graphicsMemorySize;
                _userProperties["application_version"] = Application.version;
                _userProperties["unity_version"] = Application.unityVersion;
                _userProperties["platform"] = Application.platform.ToString();
                _userProperties["environment"] = GetEnvironment();
            }
            catch (Exception e)
            {
                Debug.LogError($"[Analytics] Failed to set initial user properties: {e.Message}");
            }
        }
        
        /// <summary>
        /// Gets the current environment (Development, Editor, Production).
        /// </summary>
        private string GetEnvironment()
        {
            #if DEVELOPMENT_BUILD
            return "Development";
            #elif UNITY_EDITOR
            return "Editor";
            #else
            return "Production";
            #endif
        }
        
        /// <summary>
        /// Tracks a custom event with parameters.
        /// </summary>
        public void TrackEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            if (!_isInitialized || string.IsNullOrEmpty(eventName))
            {
                if (!string.IsNullOrEmpty(eventName))
                {
                    Debug.LogWarning($"[Analytics] Cannot track event (not initialized): {eventName}");
                }
                return;
            }
            
            try
            {
                var eventData = MergeParameters(parameters);
                
                // Add user properties to event
                foreach (var prop in _userProperties)
                {
                    if (!eventData.ContainsKey(prop.Key))
                    {
                        eventData[prop.Key] = prop.Value;
                    }
                }
                
                // Track event using Unity Analytics
                var result = Analytics.CustomEvent(eventName, eventData);
                
                if (result == AnalyticsResult.Ok)
                {
                    Debug.Log($"[Analytics] Event tracked: {eventName}");
                }
                else
                {
                    Debug.LogWarning($"[Analytics] Failed to track event {eventName}: {result}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Analytics] Exception while tracking event {eventName}: {e.Message}");
            }
        }
        
        /// <summary>
        /// Reports an exception/error to Analytics.
        /// </summary>
        public void ReportException(Exception exception, Dictionary<string, object> context = null)
        {
            if (!_isInitialized || exception == null)
            {
                if (exception != null)
                {
                    Debug.LogWarning($"[Analytics] Cannot report exception (not initialized): {exception.Message}");
                }
                return;
            }
            
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "exception_type", exception.GetType().Name },
                    { "exception_message", exception.Message },
                    { "exception_stack_trace", exception.StackTrace ?? "" }
                };
                
                if (context != null)
                {
                    foreach (var kvp in context)
                    {
                        parameters[kvp.Key] = kvp.Value;
                    }
                }
                
                TrackEvent("exception_occurred", parameters);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Analytics] Failed to report exception: {e.Message}");
            }
        }
        
        /// <summary>
        /// Reports a custom error message to Analytics.
        /// </summary>
        public void ReportError(string message, Dictionary<string, object> context = null)
        {
            if (!_isInitialized || string.IsNullOrEmpty(message))
            {
                return;
            }
            
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "error_message", message },
                    { "error_type", "CustomError" }
                };
                
                if (context != null)
                {
                    foreach (var kvp in context)
                    {
                        parameters[kvp.Key] = kvp.Value;
                    }
                }
                
                TrackEvent("error_occurred", parameters);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Analytics] Failed to report error: {e.Message}");
            }
        }
        
        /// <summary>
        /// Sets user properties for analytics.
        /// </summary>
        public void SetUserProperties(string playerId, Dictionary<string, object> properties = null)
        {
            if (!_isInitialized)
            {
                return;
            }
            
            try
            {
                _playerId = playerId;
                
                // Set user ID in Analytics
                if (!string.IsNullOrEmpty(playerId))
                {
                    // Use UGS ExternalUserId instead of obsolete SetUserId APIs
                    UnityServices.ExternalUserId = playerId;
                    _userProperties["user_id"] = playerId;
                    _userProperties["user_set_time"] = DateTime.UtcNow.ToString("O");
                }
                
                // Add additional properties
                if (properties != null)
                {
                    foreach (var prop in properties)
                    {
                        _userProperties[prop.Key] = prop.Value;
                    }
                }
                
                Debug.Log($"[Analytics] User properties set: {playerId}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Analytics] Failed to set user properties: {e.Message}");
            }
        }
        
        /// <summary>
        /// Tracks a screen view/page view.
        /// </summary>
        public void TrackScreenView(string screenName, Dictionary<string, object> parameters = null)
        {
            if (!_isInitialized || string.IsNullOrEmpty(screenName))
            {
                return;
            }
            
            try
            {
                var eventParams = new Dictionary<string, object>
                {
                    { "screen_name", screenName }
                };
                
                if (parameters != null)
                {
                    foreach (var kvp in parameters)
                    {
                        eventParams[kvp.Key] = kvp.Value;
                    }
                }
                
                TrackEvent("screen_view", eventParams);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Analytics] Failed to track screen view: {e.Message}");
            }
        }
        
        /// <summary>
        /// Tracks a purchase transaction.
        /// </summary>
        public void TrackPurchase(string productId, decimal amount, string currency, Dictionary<string, object> parameters = null)
        {
            if (!_isInitialized || string.IsNullOrEmpty(productId))
            {
                return;
            }
            
            try
            {
                var eventParams = new Dictionary<string, object>
                {
                    { "product_id", productId },
                    { "amount", amount },
                    { "currency", currency ?? "USD" }
                };
                
                if (parameters != null)
                {
                    foreach (var kvp in parameters)
                    {
                        eventParams[kvp.Key] = kvp.Value;
                    }
                }
                
                TrackEvent("purchase", eventParams);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Analytics] Failed to track purchase: {e.Message}");
            }
        }
        
        /// <summary>
        /// Tracks a level/progression event.
        /// </summary>
        public void TrackLevel(string levelName, int levelIndex, Dictionary<string, object> parameters = null)
        {
            if (!_isInitialized || string.IsNullOrEmpty(levelName))
            {
                return;
            }
            
            try
            {
                var eventParams = new Dictionary<string, object>
                {
                    { "level_name", levelName },
                    { "level_index", levelIndex }
                };
                
                if (parameters != null)
                {
                    foreach (var kvp in parameters)
                    {
                        eventParams[kvp.Key] = kvp.Value;
                    }
                }
                
                TrackEvent("level", eventParams);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Analytics] Failed to track level: {e.Message}");
            }
        }
        
        /// <summary>
        /// Merges additional parameters with user properties and default parameters.
        /// </summary>
        private Dictionary<string, object> MergeParameters(Dictionary<string, object> additionalParameters)
        {
            var merged = new Dictionary<string, object>();
            
            // Add timestamp
            merged["timestamp"] = DateTime.UtcNow.ToString("O");
            
            // Add user ID if available
            if (!string.IsNullOrEmpty(_playerId))
            {
                merged["user_id"] = _playerId;
            }
            
            // Add additional parameters
            if (additionalParameters != null)
            {
                foreach (var param in additionalParameters)
                {
                    merged[param.Key] = param.Value;
                }
            }
            
            return merged;
        }
        
        /// <summary>
        /// Shuts down the Analytics service.
        /// </summary>
        public async UniTask ShutdownAsync()
        {
            if (!_isInitialized)
            {
                return;
            }
            
            try
            {
                Debug.Log("[Analytics] Shutting down Analytics service...");
                
                // Track shutdown event
                TrackEvent("analytics_shutdown");
                
                // Clear user properties
                _playerId = null;
                _userProperties.Clear();
                
                _isInitialized = false;
                
                Debug.Log("[Analytics] Analytics service shut down.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Analytics] Error during shutdown: {e.Message}");
            }
            
            await UniTask.CompletedTask;
        }
    }
}
