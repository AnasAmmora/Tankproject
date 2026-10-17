# واجهة اللعب ومصادر بياناتها

## سلوك التلقيم وتأكيد الجاهزية الحالي

ReloadProgress وWeaponReady مخفيان افتراضيًا في Player.prefab. يقرأ PlayerUI تقدم التلقيم من CooldownRemaining كالمعتاد. WeaponReady يظهر فقط عند انتهاء تلقيم رُصد أثناء تفعيل الواجهة وبقاء موعد انتهائه دون إعادة ضبط، مع سماح السلاح بالإطلاق. CooldownEndsAt قراءة فقط لمتغير nextFireTime الموجود؛ لم يُضف مؤقت تلقيم آخر أو يتغير سلوك الإطلاق.

تأكيد الجاهزية محلي للمالك: يبدأ من الصفر ويكبر إلى مقياسه الأصلي خلال نصف ثانية ثم يصغر إلى الصفر خلال نصف ثانية، وبعد ثانية إجمالية يختفي. تُلغى الحركة السابقة ويُضبط المقياس على الصفر قبل كل ظهور. بدء تلقيم جديد أو تعطيل UI أو الموت أو النتائج يلغي التأثير. إعادة التفعيل تقرأ حالة التلقيم الحالية دون إعادة تشغيل تأكيد لتلقيم انتهى أثناء التعطيل. يستخدم التأثير unscaled Update مثل بقية مؤثرات الواجهة.

التحقق لهذا التعديل: نجح تجميع C# بمراجع Unity المثبتة و87 تحققًا بالمحاكاة، بما فيها الإلغاء وإعادة ضبط موعد التلقيم وتسلسل المقياس ومدة الثانية والإعادة والتنظيف. تأكدت حالة العنصرين ومراجع Inspector في ملف Prefab. محاولة Play Mode الحالية انتهت برمز 1 قبل بدء الاختبار؛ لا يُعتبر هذا التأثير مختبرًا بصريًا أو شبكيًا داخل Unity. سجلها `%TEMP%/TankProject-ReadyPulsePlayMode.log` في مجلد Temp الأصلي للمستخدم.

## الحفظ والتحقق الفعلي داخل Unity — المراجعة الأحدث

في بداية هذه المراجعة كان PlayerUI مركبًا بالفعل على Root اللاعب لكن كل مراجعه فارغة، وكان PlayerCanvas يحتوي Crosshair فقط ومقياسه صفرًا. أُضيفت العناصر إلى **نفس** Player.prefab، ورُبط مكوّن PlayerUI الموجود دون إضافة نسخة ثانية. أسماء مؤشرات الضرر الآن DamageBearing0 وDamageBearing1 وDamageBearing2.

شُغّل Unity 6000.4.7f1 فعليًا خارج قيود العزل. شُغّل HudAssetAudit.SaveAndReopen: حمّل محتوى Player.prefab وحفظه عبر PrefabUtility ثم أفرغه وأعاد استيراده وفتحه. النتيجة: PlayerUI واحد، أربعة أبناء مباشرين لـPlayerCanvas، وصفر أخطاء مراجع/Missing Scripts في فحص Player وMatchCanvas وصفوفهما. لم يتغير تصميم Results.

شُغّل HudPlayModeCheck.Run داخل Play Mode فعلي، في GameScene المحفوظ، وباستخدام NetworkManager وPlayer prefab الأصليين:

- نجح StartHost على اتصال محلي، وظهر Canvas المالك وحالة الجاهزية.
- استُدعي مسار TryFire الفعلي، فتغيرت حلقة التلقيم وأكملت مع إمكانية الإطلاق.
- تغيّرت الصحة المعروضة إلى 75 / 100 بعد كتابة قيمة الصحة من السيرفر في عينة الاختبار.
- اختفت عناصر Combat والقدرة عند حالة الموت، وبقي Canvas فعالًا لمعلومات الحياة.
- عادت عناصر Combat والصحة 100 / 100 بعد استدعاء RestoreOnServer الحقيقي.
- أخفت النتائج PlayerCanvas وGameplay، وبقي اللاعب فعالًا ومولودًا شبكيًا.

حدود هذا الاختبار: Host محلي واحد، مع تهيئة اتصال مباشرة وحقن حالة تجهيز/مباراة/صحة/نتيجة من السيرفر في عينة Editor فقط. لم يمر عبر Lobby/Relay، ولم ينفذ اتصال Client مستقلًا، أو إصابة تصادم حقيقية، أو مسار نقل/إقرار Respawn الكامل، أو فحصًا بصريًا للشاشة؛ التشغيل استخدم nographics. لم تُعدَّل إعدادات NetworkManager أو GameScene المحفوظة لتشغيل العينة. سجلا التنفيذ: `%TEMP%/TankProject-SaveReopenHud.log` و`%TEMP%/TankProject-HudPlayMode.log`.

صحّحنا أيضًا GUID غير صالح بطول 33 في HudAssetAudit.cs.meta؛ كان يمنع Unity من استيراد أداة الفحص نفسها. كل GUIDs تحت Assets/ProjectsFiles فُحصت للتأكد من صحتها.

مسارات PlayerCanvas المحفوظة:

```text
Player [PlayerUI + PlayerCanvasController]
└── PlayerCanvas
    ├── Combat
    │   ├── Crosshair
    │   ├── ReloadTrack / ReloadProgress / WeaponReady
    │   └── DamageBearing0 / DamageBearing1 / DamageBearing2 [DamageIndicatorView]
    │       └── Arrow
    ├── ConfirmedHit
    │   ├── HitMarker / KillMarker
    │   └── KillConfirmation
    ├── PlayerHealth
    │   ├── HealthValue
    │   ├── HealthTrack/HealthFill
    │   └── LowHealthWarning
    └── SpecialAbilityUnavailable
        ├── AbilityLabel
        ├── FutureIcon/AbilityIcon
        ├── FutureCooldown [inactive]
        └── Unavailable
```

العناصر العامة محفوظة في `MatchCanvas/Gameplay`: TimerCard/Timer وPhaseLabel، LifeCard/LifeStatus، TeammatePanel/TeammateRows، BaseDestroyedNotice، OvertimeNotice، KillFeedRows. يظل GameplayHud على Root MatchCanvas، وتستخدم الصفوف TeammateRow.prefab وKillFeedRow.prefab. لا يلزم ربط Inspector يدوي.

## سجل المراجعة السابقة قبل نجاح تشغيل Unity

السبب المثبت عند بدء هذه المراجعة: `NetworkManager.prefab` يشير إلى `Prefabes/Player.prefab` نفسه، لكن هذا الملف كان يحتوي Crosshair فقط داخل PlayerCanvas، ولم يكن يحتوي مكوّن PlayerUI على اللاعب أو مستندات عناصر HUD الجديدة. وكان مقياس PlayerCanvas المحفوظ (0,0,0). وجود PlayerUI.cs على القرص لم يكن يعني إرفاقه باللاعب. لا يثبت الفحص أي برنامج أو عملية أعادت حفظ الملف بهذه الحالة.

أُكمل تصميم PlayerCanvas وربطه داخل الـPrefab الذي تنشئه MatchManager عبر NetworkConfig.PlayerPrefab. لا يوجد Player instance محفوظ في GameScene ليطبّق التصميم عليه؛ اللاعب يُنشأ شبكيًا أثناء التجهيز. توجد نسخة واحدة من MatchCanvas في GameScene، ولا تحذف Overrides المحفوظة عناصر الواجهة. لم يُفحص أي Override غير محفوظ داخل جلسة Editor.

إضافة إلى إصلاح الأصل: PlayerUI يعيد الاشتراك وقراءة الحالة الحالية بعد إعادة تفعيله أو تأخر الملكية، ويفك الاشتراكات عند فقد الملكية. GameplayHud يصالح مرجع PlayerHealth للزميل بعد تسجيل PlayerObject، حتى إن سبقه حدث Spawned. لا ينشئ هذا المسار صفًا آخر للزميل.

الفحص الحالي: 71 تحققًا لمحاكاة الواجهة، و19 تحققًا لمحاكاة الضرر المؤكد، و5772 تحققًا نصيًا للأصول والمراجع والشجرة. نجح تجميع C# بمراجع Unity المثبتة، بما فيها فاحص Editor. هذه ليست اختبارات Play Mode أو شبكة حقيقية. محاولة تشغيل Unity للفحص انتهت برمز 1 مع Windows assertions قبل تشغيل HudAssetAudit؛ سجلها `%TEMP%/TankProject-HudAudit-20261017.log`.

من Unity استخدم `Tools > TankProject > Audit authored gameplay HUD`. يفحص الأصول المستوردة ومراجع Inspector دون تغييرها، ويطبع `[HudAssetAudit]` مع عدد الأخطاء. ينفّذ مرة واحدة أيضًا عند تحميل سكربتات Editor في جلسة جديدة، خارج Play Mode. هذا الفحص لا يشغّل المباراة.

| المتطلب | التصميم المحفوظ | البيانات / السكربت | الربط | اختبار تشغيل Unity |
|---|---|---|---|---|
| التلقيم | Player.prefab / PlayerCanvas / Combat / ReloadProgress وWeaponReady | PlayerUI ← TankShooter.CooldownRemaining وCanFire | مكتمل | لم يُنفذ |
| الصحة | PlayerCanvas / PlayerHealth / HealthValue وHealthTrack/HealthFill | PlayerUI ← PlayerHealth.currentHealth وmaxHealth | مكتمل | لم يُنفذ |
| القدرة المؤجلة | PlayerCanvas / SpecialAbilityUnavailable | Button غير متاح، FutureIcon/AbilityIcon وFutureCooldown للمستقبل | مكتمل | لم يُنفذ |
| اتجاه الضرر | PlayerCanvas / Combat / DamageBearing (ثلاثة) / Arrow | PlayerUI وDamageIndicatorView ← CombatConfirmed.Source وكاميرا اللاعب | مكتمل | لم يُنفذ |
| تدمير القاعدة | MatchCanvas.prefab / Gameplay / BaseDestroyedNotice | GameplayHud ← destroyedBases وفريق اللاعب، بما فيه Overtime | مكتمل | لم يُنفذ |
| الزميل | Gameplay / TeammatePanel / TeammateRows، مع TeammateRow.prefab | GameplayHud وTeammateRow ← statistics وPlayerHealth للزميل | مكتمل | لم يُنفذ |
| تأكيد الإصابة والقتل | PlayerCanvas / ConfirmedHit / HitMarker وKillMarker وKillConfirmation | PlayerUI ← CombatConfirmed المؤكد من السيرفر، مع تسلسل منع التكرار | مكتمل | لم يُنفذ |
| الوقت الإضافي | Gameplay / OvertimeNotice | GameplayHud ← MatchManager.phase | مكتمل | لم يُنفذ |
| Kill Feed | Gameplay / KillFeedRows، مع KillFeedRow.prefab | GameplayHud وKillFeedRow ← CombatConfirmed.IsKill | مكتمل | لم يُنفذ |
| الوقت والحياة | Gameplay / TimerCard / Timer وPhaseLabel، وLifeCard/LifeStatus | GameplayHud وHudPresentation ← phaseEndsAt وServerTime وحالة الحياة الفعلية | مكتمل | لم يُنفذ |

لا يلزم ربط Inspector جديد للملفات المحفوظة. بعد استيراد التغييرات افتح Player.prefab الفعلي وتحقق من أبناء PlayerCanvas الأربعة (Combat وConfirmedHit وPlayerHealth وSpecialAbilityUnavailable) ومن PlayerUI على Root اللاعب. أعد بناء نسخ الاختبار من الأصول المحدثة؛ ملف تنفيذي قديم لن يتلقى تعديل Prefab من تلقاء نفسه. نفّذ مصفوفة التشغيل في نهاية هذا الملف قبل اعتماد نجاح الواجهة.

التصميم محفوظ في `Player.prefab` و`MatchCanvas.prefab`، مع صفوف `TeammateRow.prefab` و`KillFeedRow.prefab`. لا ينشئ الكود Canvas أو Text أو Image أو تخطيطًا أثناء التشغيل. لا توجد حزمة DOTween في المشروع؛ الانتقالات محلية قصيرة تعتمد على fillAmount وCanvasGroup.alpha.

| العنصر | مصدره الفعلي |
|---|---|
| حلقة التلقيم | `TankShooter.CooldownRemaining / CooldownDuration` من نفس `nextFireTime` الذي يستخدمه الإطلاق. `CanFire` هو شرط TryFire نفسه، ويشمل جاهزية اللاعب والمباراة. |
| الصحة | `PlayerHealth.currentHealth` و`maxHealth` على اللاعب المحلي. النص فوري والشريط ينتقل بصريًا؛ يتوقف التحذير عند الموت وتُعاد قراءة الصحة عند العودة. |
| القدرة الخاصة | خانة Editor فقط: Button غير قابل للاستخدام، Image فارغ للأيقونة المستقبلية، ومؤشر Cooldown مخفي. لا يوجد Input أو قدرة أو توقيت مصطنع. |
| اتجاه الضرر | موضع إطلاق الرصاصة المحفوظ في Bullet عند Launch. يُرسَل مع تأكيد السيرفر بعد قبول الضرر، ثم يُحوّل إلى اتجاه نسبة إلى كاميرا اللاعب الحالية. ثلاثة مؤشرات تدعم الإصابات المتتابعة وتُمسح عند العودة/النتائج. لا يُختلق اتجاه إذا لم يتوفر المصدر. |
| تدمير القاعدة | بت فريق اللاعب في `MatchManager.destroyedBases`، مع قناع محلي لمنع تكرار الإشعار. يدعم انتهاء الوقت الأساسي لأنه يستخدم العلم الدائم نفسه. |
| الزميل | هوية وفريق وسجل الانقطاع من `MatchManager.statistics`، والصحة وحالة الموت/موعد العودة من PlayerHealth الخاص بالزميل عبر clientId. يختفي القسم في 1v1. يربط Health المتأخر عبر حدث Spawned. |
| Hit/Kill confirmation | حدث CombatConfirmed من MatchManager. يصدره السيرفر فقط بعد قبول الضرر؛ القتل يصدر بعد نجاح MatchRules.RecordDeath. Kill للّاعب فقط، وإصابة القاعدة تؤكد Hit دون Kill. معرّف تسلسلي يمنع تكرار الاستلام. |
| Kill Feed | نفس تأكيد القتل، بأسماء وهويات الفريقين من السجل المحفوظ على السيرفر. لا يخترع قاتلًا عند غيابه، ولا يعتبر ضررًا عاديًا أو تدمير قاعدة قتلًا. عدد الصفوف ومدتها قابلان للتعديل. |
| Overtime | `MatchManager.phase` عند الوصول إلى Overtime، مرة واحدة لكل واجهة مباراة. |
| Timer / LifeStatus | phaseEndsAt ناقص NetworkManager.ServerTime؛ الصحة/الموت/respawnAt والفريق وأعلام القواعد من حالتها الحالية. الواجهة تنسّق هذه القيم ولا تقرر العودة أو الفوز. |

`PlayerUI` على Root اللاعب يعرض واجهة المالك ويربط الأحداث. `GameplayHud` على MatchCanvas يعرض الواجهة العامة دون تكرارها لكل دبابة. `PlayerCanvasController` يحتفظ بمسؤولية إظهار Canvas المالك وإخفائه عند النتائج. إشعار القتل منفصل عن عناصر التصويب، ليبقى تأكيد المقذوف الذي وصل بعد موت مطلقه ممكنًا. النتائج تخفي جميع واجهات اللعب والتنبيهات دون تعطيل الشبكة أو اللاعب.

لم يتغير تصميم Results أو ترتيب عناصره أو صفوفه. في مراجعة التعديل قورنت جميع مستندات عناصره الـ95 بالمصدر السابق وبقيت متطابقة، باستثناء فروقات المسافات البيضاء غير المؤثرة.

## الربط في Inspector

جميع المراجع اللازمة رُبطت في الملفات المحفوظة. لا توجد خطوة ربط يدوية مطلوبة. يمكن تعديل المظهر والمدد من:

- Player / PlayerUI: عتبة الصحة المنخفضة وسرعة انتقال الشريط، ومراجع الصحة والسلاح والمؤشرات.
- كل DamageBearing / DamageIndicatorView: مدة الإظهار.
- MatchCanvas / GameplayHud: ألوان الوقت، عتبة الوقت المنخفض، مدة التنبيهات، عدد صفوف Kill Feed ومدتها، والـPrefabs.
- TeammateRow وKillFeedRow: الخلايا والخطوط والألوان وLayout Groups.
- SpecialAbilityUnavailable: عناصر الأيقونة والزر ومؤشر Cooldown جاهزة للتطوير لاحقًا، دون تشغيلها الآن.

أعد بناء نسختي الاختبار معًا بسبب إضافة RPC إلى MatchManager. لا حاجة لتغيير الحزم أو إعدادات الصلاحيات أو معدلات الشبكة.

## ما نُفّذ آليًا

```powershell
dotnet run --project Tests/UI/UI.Tests.csproj
dotnet run --project Tests/Combat/Combat.Tests.csproj
dotnet run --project Tests/MatchRules/MatchRules.Tests.csproj
dotnet run --project Tests/LobbyFlow/LobbyFlow.Tests.csproj
python Tests/UI/verify_assets.py
```

- اختبارات UI تجمع سكربتات العرض الفعلية مع محاكاة المحرك: صحة محلية وزميل بصحة قصوى مختلفة، سلاسة الشريط، التلقيم، اتجاه يتغير مع الكاميرا وإصابات متتابعة، تأكيد إصابة/قتل، قتل دون قاتل، حد الصفوف وانتهاؤها، 1v1 و2v2، العد التنازلي والإقصاء والانقطاع، منع تكرار تنبيهات القاعدة وOvertime، الموت والعودة والنتائج وتنظيف الاشتراكات.
- اختبارات Combat تجمع PlayerHealth وTeamBase وMatchManager وMatchRules الفعلية، مع محاكاة المحرك وتوصيل RPC: رفض الضرر أثناء التجهيز وعلى العميل والإصابة الصديقة والضرر الصفري، مصدر الضربة، القتل مرة واحدة، إعادة استلام نفس التسلسل، قتل دون قاتل، وإصابة/تدمير قاعدة دون زيادة القتلى. تُفحص هويات Host→Client وClient→Host وClient→Client كبيانات على السيرفر؛ ليست اتصالات فعلية.
- اختبارات المراجع تفحص IDs وربط الـPrefabs وأنواع المكونات، وليس استيراد الأصول أو رسمها بمحرك Unity.

## الاختبار داخل Unity

هذه المصفوفة تحتاج تنفيذًا فعليًا؛ لا تثبتها اختبارات المحاكاة أو نجاح التجميع:

1. شغّل Host وClient بهويتين مختلفتين. ادخل من القائمة حتى جاهزية المباراة. تحقق أن التلقيم لا يعرض Ready قبل إمكانية الإطلاق، وأن الفاصل بين الطلقات يطابق الحلقة. أعد التجربة بمدة fireCooldown مختلفة من Inspector.
2. اختبر Host→Client وClient→Host، ثم أربعة لاعبين وClient→Client. قارن HP الفعلية بنص/شريط الصحة عند جميع الجهات. الإصابة الصديقة أو اصطدام الجدار لا يظهر Hit Marker، وإصابة القاعدة تظهر Hit فقط.
3. أصب اللاعب من الأمام واليمين والخلف، ثم لف كاميرته أثناء بقاء المؤشر. نفّذ إصابات متقاربة من جهات مختلفة. لا يظهر مؤشر جديد دون تغير ضرر مقبول، وتختفي المؤشرات القديمة.
4. اقتل خصمًا مرة واحدة وراقب الاسم واللون في Kill Feed، وتأكيد القتل عند المطلق فقط. جرّب قتلًا بمقذوف يصل بعد موت المطلق، وتدمير قاعدة، ووفاة من مصدر غير معروف إن توفّر مسار اختباري مخوّل. لا تعدّل صحة عميل لتوليد تأكيد مزيف.
5. في 1v1 لا توجد لوحة زميل. في 2v2 تابع الاسم وصحة الزميل، ثم موته وعدّه التنازلي وعودته وإقصاءه بعد فقد القاعدة. اقطع اتصال الزميل؛ ينبغي أن يعرض Disconnected دون استخدام صحة اللاعب المحلي.
6. دمّر قاعدة فريق واحد: يظهر التنبيه لأعضائه مرة واحدة، ويبقى عدّ الرجوع مطابقًا لقواعد المباراة. قصّر regulationDuration للاختبار، وتحقق من انتقال Overtime وإتلاف القاعدتين ومنع العودة وفق النظام الحالي. لا تُبنَ قاعدة حياة داخل UI.
7. عند الموت تختفي علامة التصويب/التلقيم وخانة القدرة، وتبقى الصحة وحالة الحياة وبيانات المباراة. عند العودة تُمسح مؤشرات الضرر القديمة وتُقرأ الصحة والتلقيم الحاليان.
8. عند Results تختفي كل لوحات اللعب والإشعارات وPlayerCanvas، ويبقى Results مطابقًا لتصميمه وزر Back فعّالًا. ارجع وابدأ مباراة ثانية؛ لا تعود صفوف أو تنبيهات أو اشتراكات قديمة.
9. كرر أثناء الحركة ومع تأخير شبكة من أدوات Unity Multiplayer. افحص 1080p و1440p و4K و16:10 و21:9، وتأكد من وضوح النص وعدم تداخل التنبيهات والـKill Feed والتصويب.

لم يُنفّذ تشغيل متعدد اللاعبين أو فحص بصري لهذه الواجهة داخل Unity في هذه الجلسة؛ يجب تسجيل نتائجه بصورة مستقلة عن الاختبارات الآلية أعلاه.

محاولة تشغيل Unity في batchmode خرجت بالرمز 1 قبل استيراد/فحص المشروع، مع رفض الاتصال بقناة LicensingClient ورسائل Assertion من Windows. سجل المحاولة: `%TEMP%/TankProject-Hud-UnityCheck.log`. لذا لم يُتحقق من NGO IL post-processing أو استيراد/عرض الـPrefabs داخل المحرر هنا؛ فحص C# المنفصل يستخدم مراجع الحزم المثبّتة فقط.
