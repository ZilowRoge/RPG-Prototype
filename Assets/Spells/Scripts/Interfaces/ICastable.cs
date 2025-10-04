using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Player.Statistics;

namespace Spells.Interfaces {
    public interface ICastable {
        void OnDeactivation(CasterData stats);
        void OnActivation(CasterData stats);
        void OnCast(CasterData stats);
    }
}