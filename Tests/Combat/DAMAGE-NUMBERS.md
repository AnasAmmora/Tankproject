تم تنفيذ تناقص الضرر وأرقام الضرر المحلية، مع استخدام مسار تأكيد الإصابة وPoolManager الموجودين. أُنشئت الأصول ورُبطت مراجعها وحُفظت عبر Unity MCP في Unity 6000.4.7f1.

| الملف | المسؤولية |
|---|---|
| `Assets/ProjectsFiles/Scripts/DamageFalloff.cs` — جديد | ScriptableObject قابل لإعادة الاستخدام: إعداد الضرر، تنظيف القيم، حساب المسافة والتناقص والتقريب. |
| `Assets/ProjectsFiles/Scripts/Player/Bullet.cs` | يستخدم إعداد الضرر على السيرفر عند الاصطدام المؤكد. يحتفظ بموضع الإطلاق عند Launch ويصفّره عند OnDisable، ويمرر نقطة الإصابة إلى مستقبل الضرر. تبقى آلية منع تكرار المقذوف وسلوكه الفيزيائي كما كانت. |
| `Assets/ProjectsFiles/Scripts/Player/PlayerHealth.cs` | يخصم الحد الأدنى بين الضرر المحسوب والصحة المتبقية، ويرسل المقدار الفعلي مع التأكيد العادي أو القاتل؛ تبقى قواعد الفريق ونسبة القتل للسيرفر. |
| `Assets/ProjectsFiles/Scripts/TeamBase.cs` | يطبق السياسة نفسها على القواعد ويرفض الضرر الإضافي بعد نفاد الصحة أو التدمير. |
| `Assets/ProjectsFiles/Scripts/MatchManager.cs` | يوسّع RPC التأكيد الموجود. يرسل الضرر الفعلي ونقطة الإصابة للرامي فقط، ويرسل تأكيدًا دون هذه البيانات لبقية الاتصالات. المجموعتان منفصلتان، ويستخدم كل حدث رقمًا تسلسليًا واحدًا. |
| `Assets/ProjectsFiles/Scripts/CombatFeedback.cs` | يحمل AppliedDamage وHasImpact وImpactPosition، ويوفر شرط أهلية العرض للرامي المحلي. |
| `Assets/ProjectsFiles/Scripts/FloatingDamageNumber.cs` — جديد | يحرك نص TMP المصمم في Prefab: تكبير سريع، صعود، تلاشي، مواجهة الكاميرا، وحجم مقروء بالبكسل. يصفر حالة الحركة والنص عند إعادته للمخزن. لا يحتوي حساب ضرر أو RPC. |
| `Assets/ProjectsFiles/Scripts/GameplayHud.cs` | ينشئ نسخًا محلية من الـPrefab عبر PoolManager بعد التأكيد فقط، ويحتفظ بكل إصابة مستقلة، ويعيدها عند انتهاء العمر أو النتائج أو إلغاء الربط. ترتيب التنفيذ 300 يجعل مواجهة النص بعد وضع الكاميرا النهائي في TankCameraUpdate ذي الترتيب 200. |
| `Assets/ProjectsFiles/Editor/DamageFeatureChecks.cs` — جديد | فحوص إعدادات وأصول محفوظة، واختبار Host محلي فعلي، يُستدعيان صراحة عبر Unity MCP. يعيد المشهد السابق دون حفظ حالة الاختبار. |
| `Tests/Combat/Program.cs` | يضيف فحوص الصحة المتبقية والضربات القاتلة والأهداف المنفصلة وشرط الرامي وضربات متتابعة. |
| `Tests/Combat/ProjectileTests.cs` و`ProjectileBoundary.cs` | يفحصان Bullet الفعلي مقابل حدود محاكاة: القريب والمتوسط والبعيد، إعادة استخدام المقذوف، القواعد، الحائط، ومنع تكرار المعالجة. |
| `Tests/LobbyFlow/Fakes.cs` و`Tests/UI/GameplayTests.cs` | يواكبان مراجع العرض وواجهات اختيار أهداف RPC الجديدة في بيئة المحاكاة. لا ينقلان رسائل شبكة حقيقية. |
| `Tests/Combat/Projectile.Tests.csproj` | يضم DamageFalloff الفعلي إلى تجميع اختبارات المقذوف. |

المعادلة، حيث d هي المسافة المستقيمة بين موضع إطلاق المقذوف نفسه ونقطة اصطدامه المؤكدة:

```text
t = Clamp01((d - fullDamageDistance) / (maximumFalloffDistance - fullDamageDistance))
s = t² × (3 - 2t)
rawDamage = baseDamage × (1 + (minimumDamageMultiplier - 1) × s)
calculatedDamage = Clamp(Floor(rawDamage + 0.5), 0, baseDamage)
appliedDamage = Min(calculatedDamage, remainingHealth)
```

التقريب إلى أقرب عدد صحيح، والنصف إلى الأعلى، مرة واحدة بعد التناقص. الصفر مسموح؛ إذا كانت النتيجة صفرًا فلا يتأكد ضرر ولا يظهر رقم. لا يُقاس طول القوس ولا يُقرأ موضع الرامي عند الإصابة. لا يرسل العميل مقدار ضرر موثوقًا إلى السيرفر.

الإعداد الافتراضي محفوظ في `Assets/ProjectsFiles/Settings/StandardShellDamage.asset`:

| Inspector | القيمة | المعنى |
|---|---:|---|
| Base Damage | 25 | الضرر قبل التناقص. |
| Full Damage Distance | 20 | قوة كاملة حتى هذه المسافة. |
| Maximum Falloff Distance | 80 | يصل التناقص عندها إلى الحد الأدنى. |
| Minimum Damage Multiplier | 0.4 | نسبة الضرر عند المسافة القصوى وما بعدها. |

أمثلة: 5 و20 وحدة ← 25؛ 35 ← 23؛ 50 ← 18؛ 65 ← 12؛ 80 و120 ← 10. عند 50 وحدة تكون القيمة الخام 17.5 فتقرب إلى 18؛ إذا بقيت 6 صحة فقط فالخصم والرقم كلاهما 6.

التحقق من الإعدادات يعمل في Inspector وعند الحساب: ضرر غير سالب، معامل محصور بين صفر وواحد، وتنظيف القيم غير المحدودة. مسافة القوة الكاملة محصورة حتى 100000، ومسافة النهاية حتى 1000000، وتبقى النهاية أكبر من البداية بهامش لا يقل اسميًا عن 0.01 وحدة، لحماية الحساب من نطاق صفري ودقة float. مسافة NaN تعطي صفرًا، والمسافة الموجبة غير المحدودة تصل إلى الحد الأدنى.

المسار الكامل: TankShooter يطلق بالطريقة الموجودة ← Bullet.Launch يلتقط موضع الإطلاق لكل استخدام ← السيرفر يحسم الاصطدام مرة واحدة ويحسب DamageFalloff ← PlayerHealth أو TeamBase يخصم الصحة الفعلية ← MatchManager يرسل التأكيد الموجود مع بيانات الرقم لاتصال الرامي فقط ← GameplayHud المحلي يتحقق من هوية الرامي والمقدار الموجب ونقطة الإصابة ← PoolManager يوفر نسخة محلية من FloatingDamageNumber. لا تتصل المؤثرات المتوقعة بهذا المسار، ولا يُنشأ NetworkObject للأرقام. استدعاء RPC على الـHost المحلي مسموح، دون استثناء IsServer يحجب عرضه.

إعداد الـPrefab المنفذ فعليًا:

```text
FloatingDamageNumber [FloatingDamageNumber]
└── DamageText [TextMeshPro + MeshRenderer]
```

الـPrefab محفوظ في `Assets/ProjectsFiles/Prefabes/UI/FloatingDamageNumber.prefab`. مرجع label مربوط بـDamageText، مع خط LiberationSans SDF محفوظ، دون Canvas أو NetworkObject. مرجع GameplayHud.floatingDamageNumberPrefab مربوط في MatchCanvas.prefab، وفُحصت وراثة هذا المرجع في نسخة GameScene الوحيدة. مرجع Bullet.damageFalloff مربوط بملف StandardShellDamage. لا توجد خطوة ربط يدوية متبقية.

تعديل الشكل من مكوّن FloatingDamageNumber في الـPrefab: lifetime=0.9 ثانية، riseDistance=1.2 وحدة، scale=30 بكسل كارتفاع أساسي، لون ذهبي، impactOffset=(0,0.6,0)، horizontalSpread=0.15، popScale=1.3، popDuration=0.2 ثانية، وfadeStart=0.35 كنسبة من العمر. الحجم يتكيف مع عمق الكاميرا ومجال الرؤية، ويدعم الكاميرا المتعامدة. عند غياب الكاميرا أو وجود النص خلفها يبقى مخفيًا، ويستمر عمره دون بناء واجهة بديلة.

لإضافة سلاح آخر: أنشئ Damage Falloff من قائمة Create > Tank Project، واضبط القيم واربط الأصل في Prefab المقذوف الخاص به. يمكن نسخ FloatingDamageNumber.prefab لتغيير الخط والشكل والحركة، ثم تبديل مرجع العرض في MatchCanvas. الأصوات ومؤشرات الإصابة والقتل والارتداد ورجّة الكاميرا ما زالت تستخدم مساراتها الحالية.

الفحوص المنفذة فعليًا في 31 أكتوبر 2026:

- نجح تجميع Unity النهائي دون أخطاء أو تحذيرات تجميع.
- نجح `DamageFeatureChecks.RunAssetChecks()` في 217 تحققًا داخل Unity: القيم القريبة والمتوسطة والبعيدة، الرتابة، الحدود والتقريب، الإعدادات التالفة، ومراجع الـPrefabs ونسخة GameScene المحفوظة.
- نجح `DamageFeatureChecks.StartHostSmoke()` في 50 تحققًا داخل Play Mode وNGO Host حقيقي: مسار Bullet إلى صحة القاعدة ثم RPC الرامي ثم TMP، إعادة استخدام المقذوف، الضربات الودية والمحجوبة، الضربات المتتابعة، عرض 14 رقمًا مستقلًا في الوقت نفسه، ضربة قاتلة تخصم 6 فقط، التلاشي والتكبير ومواجهة الكاميرا وإعادة ضبط المخزن والتنظيف عند النتائج. نقاط الاتصال استُدعيت اصطناعيًا، مع تهيئة قواعد المباراة والكاميرا داخل أداة الاختبار فقط.
- نجح مشروع Combat.Tests في 66 تحققًا، ومشروع Projectile.Tests في 20 تحققًا. هذه محاكاة لحدود المحرك واختيار أهداف RPC، وليست اختبار نقل شبكة بين عمليتين.
- رُسمت ثلاث نسخ من الـPrefab عبر كاميرا Unity وفُحصت صورة المعاينة: 25 أثناء التكبير، و18 بعده، و6 أثناء التلاشي. هذه معاينة منفصلة، وليست فحصًا لمباراة كاملة.
- جرت محاولة مجموعة UI.Tests؛ نجح تجميعها ثم توقفت عند fixture قديم يحاول ربط حقل weaponReady غير الموجود في PlayerUI. هذا التعارض موجود في نسخة HEAD السابقة للتعديل أيضًا، ولم تُغيّر اختبارات الميزة سلوك السلاح لإخفائه.
- نجح `git diff --check`. بقي MainMenuScene مفتوحًا دون تغييرات غير محفوظة بعد الاختبار.

لم يُوصل Client مستقل، ولم تُختبر فعليًا رؤية الرامي مقابل الضحية والزميل والمراقب عبر اتصال متعدد اللاعبين، أو التأخير وإعادة الاتصال، أو تصادم PhysX في لعب حي. عزل أهداف RPC وشرط العرض للرامي مفحوصان بالكود والمحاكاة، ومسار الرامي الـHost مفحوص فعليًا. يلزم قبل إصدار اللعبة تشغيل Host وClient مستقلين والتبديل بين الرامي والضحية، مع مراقب أو زميل إن أمكن، والتأكد من أن عدد الأرقام عند غير الرامي يظل صفرًا.

لإعادة التشغيل عبر Unity MCP execute_code في Editor نظيف:

```csharp
return DamageFeatureChecks.RunAssetChecks();
// استدعاء منفصل؛ يبدأ Play Mode ويعيد المشهد السابق عند الانتهاء:
return DamageFeatureChecks.StartHostSmoke();
// استدعاء لاحق بعد الخروج من Play Mode:
return DamageFeatureChecks.LastResult;
```

أوامر المحاكاة على Windows، مع مخرجات داخل Temp المشروع:

```powershell
$env:TEMP = Join-Path (Get-Location) 'Temp/CodexDamageTests'
New-Item -ItemType Directory -Path $env:TEMP -Force | Out-Null
dotnet run --project Tests/Combat/Combat.Tests.csproj
dotnet run --project Tests/Combat/Projectile.Tests.csproj -p:BaseOutputPath=../../Temp/ProjectileTests/bin/ -p:BaseIntermediateOutputPath=../../Temp/ProjectileTests/obj/
```
