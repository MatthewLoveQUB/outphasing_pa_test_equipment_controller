using QubVisa;

namespace OutphasingSweepController
    {
    public class Equipment
        {
        public HP6624A Hp6624a;
        public TektronixRSA3408A Rsa3408a;
        public RS_SMR20 Smr20;
        public KeysightE8257D E8257d;

        public Equipment(
            HP6624A hp6624a, 
            TektronixRSA3408A rsa3408a,
            RS_SMR20 smr20,
            KeysightE8257D e8257d)
            {
            this.Hp6624a = hp6624a;
            this.Smr20 = smr20;
            this.Rsa3408a = rsa3408a;
            this.E8257d = e8257d;
            }
        }
    }
