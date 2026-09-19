using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;

namespace Gameplay.Commands {
    /// <summary>
    /// Handles tracking event subscriptions for <see cref="IAbility"/>s.
    /// 
    /// Ability instances get changed all the time during networking, which means that abilities can not directly
    /// subscribe listeners without the invoked listener methods being on stale instances. Big bummer for when we want to
    /// modify the ability's AbilityParameter state off triggered listeners.
    ///
    /// So instead, abilities can use this to subscribe listeners, and this ensures that those listeners are invoked on
    /// the correct, fresh ability instance.
    ///
    /// Should only be used on the SERVER. No networking involved. 
    /// </summary>
    public class AbilityEventRouter {
        private class Subscription {
            public Delegate ProxyDelegate;
            public Action Unsubscribe;
        }

        private readonly Dictionary<string, List<Subscription>> _subscriptions = new();

        /// <summary>
        /// Handles retrieving an <see cref="IAbility"/> instance given an entity and ability UID. 
        /// </summary>
        private readonly Func<GridEntity, string, IAbility> _instanceLookup;

        public AbilityEventRouter(Func<GridEntity, string, IAbility> instanceLookup) {
            _instanceLookup = instanceLookup;
        }

        public void RegisterListener<TDelegate>(GridEntity entity, string uid, Action<TDelegate> subscribe, Action<TDelegate> unsubscribe, TDelegate listener)
                                                where TDelegate : Delegate {
            MethodInfo method = listener.Method;
            TDelegate proxy = CreateProxyDelegate<TDelegate>(entity, uid, method);
            subscribe(proxy);

            if (!_subscriptions.TryGetValue(uid, out List<Subscription> list)) {
                list = new List<Subscription>();
                _subscriptions.Add(uid, list);
            }

            list.Add(new Subscription {
                ProxyDelegate = proxy,
                Unsubscribe = () => unsubscribe(proxy)
            });
        }

        public void UnregisterListeners(GridEntity entity, string uid) {
            if (!_subscriptions.TryGetValue(uid, out List<Subscription> list)) return;

            foreach (Subscription subscription in list) {
                subscription.Unsubscribe();
            }

            _subscriptions.Remove(uid);
        }

        private TDelegate CreateProxyDelegate<TDelegate>(GridEntity entity, string uid, MethodInfo method) where TDelegate : Delegate {
            MethodInfo invokeMethod = typeof(TDelegate).GetMethod("Invoke");
            ParameterInfo[] parameterInfos = invokeMethod!.GetParameters();

            ParameterExpression[] parameters = new ParameterExpression[parameterInfos.Length];

            for (int i = 0; i < parameters.Length; i++) {
                parameters[i] = Expression.Parameter(parameterInfos[i].ParameterType, parameterInfos[i].Name);
            }

            MethodInfo forwardMethod = GetType().GetMethod(nameof(Forward), BindingFlags.NonPublic | BindingFlags.Instance);
            Expression[] boxedArguments = new Expression[parameters.Length];

            for (int i = 0; i < parameters.Length; i++) {
                boxedArguments[i] = Expression.Convert(parameters[i], typeof(object));
            }

            Expression argumentsArray = Expression.NewArrayInit(typeof(object), boxedArguments);
            Expression call = Expression.Call(
                Expression.Constant(this),
                forwardMethod,
                Expression.Constant(entity, typeof(GridEntity)),
                Expression.Constant(uid, typeof(string)),
                Expression.Constant(method, typeof(MethodInfo)),
                argumentsArray);

            return Expression.Lambda<TDelegate>(call, parameters).Compile();
        }

        private void Forward(GridEntity entity, string uid, MethodInfo method, object[] arguments) {
            object instance = _instanceLookup(entity, uid);
            if (instance == null) return;

            method.Invoke(instance, arguments);
        }
    }
}