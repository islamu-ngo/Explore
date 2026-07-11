using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Domain.Enums;

public enum AudienceAgeEnum
{
    AllAges = 1,           // Pas de restriction (MinAge = null, MaxAge = null)

    // Restrictions "minimum age" (entrée interdite en dessous)
    AdultsOnly18Plus = 2,  // 18+
    Teens16Plus = 3,       // 16+
    Preteens12Plus = 4,    // 12+

    // Restrictions "maximum age" (réduction/accès spécial pour jeunes)
    ChildrenUnder6 = 5,    // 0-6
    YouthUnder12 = 6,   // 0-12
    YouthUnder16 = 7,   // 0-16
    YouthUnder18 = 8       // 0-18
}
