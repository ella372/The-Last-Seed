namespace LastSeed.Domain
{
    public enum SoilState { Dead, Fertile }
    public enum PlantStage { None, Seed, Sprout, Grown }
    public enum GameStatus { Playing, AreaRestored }

    public interface IFeedbackSink
    {
        void Show(string message);
    }

    public class Inventory
    {
        public const int MaxWater = 5;
        public int Water;
        public int PioneerSeeds;
    }

    public class PlantSpot
    {
        public PlantStage Stage = PlantStage.None;
        public SoilState Soil = SoilState.Dead;
    }

    public class GameState
    {
        public Inventory Inventory = new Inventory();
        public PlantSpot PS1 = new PlantSpot();
        public GameStatus Status = GameStatus.Playing;

        public GameState() { Reset(); }

        public void Reset()
        {
            Inventory.Water = 2;
            Inventory.PioneerSeeds = 1;
            PS1.Stage = PlantStage.None;
            PS1.Soil = SoilState.Dead;
            Status = GameStatus.Playing;
        }
    }

    public class RuleEngine
    {
        private readonly GameState _s;
        private readonly IFeedbackSink _fb;

        public RuleEngine(GameState state, IFeedbackSink feedback)
        {
            _s = state;
            _fb = feedback;
        }

        
        public bool CollectWater()
        {
            if (_s.Status != GameStatus.Playing) return false;
            if (_s.Inventory.Water >= Inventory.MaxWater)
            {
                _fb.Show("Water is full (5/5)");
                return false;
            }
            _s.Inventory.Water++;
            _fb.Show("Collected: Water (" + _s.Inventory.Water + "/" + Inventory.MaxWater + ")");
            return true;
        }

        
        public bool PlantPioneer()
        {
            if (_s.Status != GameStatus.Playing) return false;
            if (_s.Inventory.PioneerSeeds <= 0) { _fb.Show("No seeds available"); return false; }
            if (_s.PS1.Stage != PlantStage.None) { _fb.Show("Already planted"); return false; }
            _s.Inventory.PioneerSeeds--;
            _s.PS1.Stage = PlantStage.Seed;
            _fb.Show("Seed planted. Needs 1 Water to Grow");
            return true;
        }

        
        public bool WaterPlant()
        {
            if (_s.Status != GameStatus.Playing) return false;
            if (_s.PS1.Stage == PlantStage.None) { _fb.Show("Nothing planted here"); return false; }
            if (_s.PS1.Stage == PlantStage.Grown) { _fb.Show("Plant is already grown"); return false; }
            if (_s.Inventory.Water <= 0) { _fb.Show("Need Water! Find a water source."); return false; }

            _s.Inventory.Water--;
            if (_s.PS1.Stage == PlantStage.Seed)
            {
                _s.PS1.Stage = PlantStage.Sprout;
                _fb.Show("Sprout!");
            }
            else
            {
                _s.PS1.Stage = PlantStage.Grown;
                _s.PS1.Soil = SoilState.Fertile;
                _s.Status = GameStatus.AreaRestored;
                _fb.Show("Plant Grown! Area 1 Restored");
            }
            return true;
        }
    }
}
