using System;
using UnityEngine;

namespace Gameplay.Config {
    /// <summary>
    /// Represents a <see cref="PurchasableData"/> that is required for making another purchase
    /// </summary>
    [Serializable]
    public class PurchasableRequirement {
        public PurchasableData Purchasable;
        [Tooltip("Either this OR the above Purchasable must be owned to fulfill this requirement. ")]
        public PurchasableData AlternativePurchasable;
        /// <summary>
        /// Whether the purchasable needs to be adjacent to the purchasing entity
        /// </summary>
        public bool MustBeAdjacent;
        public string FailedRequirementExplanation;
    }
}