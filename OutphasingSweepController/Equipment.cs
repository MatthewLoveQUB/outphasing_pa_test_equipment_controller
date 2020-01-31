using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QubVisa;

namespace OutphasingSweepController
    {
    public class Equipment
        {
        public HP6624A Hp6624a;
        public RS_SMR20 Smu200a;
        public TektronixRSA3408A Rsa3408a;
        public KeysightE8257D E8257d;

        public Equipment(
            HP6624A hp6624a, 
            RS_SMR20 smu200a, 
            TektronixRSA3408A rsa3408a, 
            KeysightE8257D e8257d)
            {
            this.Hp6624a = hp6624a;
            this.Smu200a = smu200a;
            this.Rsa3408a = rsa3408a;
            this.E8257d = e8257d;
            }
        }
    }
