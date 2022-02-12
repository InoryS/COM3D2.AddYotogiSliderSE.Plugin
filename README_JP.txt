COM3D2.AddYotogiSliderSE.Plugin
通常/互換モード両対応の夜伽スライダー
v0.0.1.6

このプラグインはasyetriec氏のCOM3D2.AddYotogiSlider.Plugin（https://github.com/asyetriec/COM3D2.AddYotogiSlider.Plugin）を
改変したものです。互換等で大幅に手を加えたため名前を変えてリリースしました。

iniファイルでStatus、FaceAnime、FaceBlendを（試験的に）初期設定できるようにしました。
詳しくは「設定ファイルについて」を見てください。

不具合報告などはしたらば改造スレでお願いします。

■更新履歴
[v0.0.1.6]
・v0.0.1.5の方とも違う素人別人がcom3d2 v2.4.0環境で再コンパイルしただけです。
　ステータス操作以外の試験を行っていません。
　src\Libsフォルダのreadmeにて再コンパイルの案内があります。
　build.batを同梱しました。（Libsフォルダの作業後に起動すれば再コンパイルが行えます。ご活用ください。）

[v0.0.1.5]
・素人別人がcom3d2 ver1.58環境で何も考えずに再コンパイルしただけです。
　一応稼働するようにはなりましたが、何か問題はあるかもしれません。
　当方では対応しかねますので、そこから先は詳しい方よろしくお願いします。

[v0.0.1.4]
・ini追加項目の仕様と反映するタイミングを変更しました。

[v0.0.1.3]
・精神・官能スライダーがスキル開始時に同期していなかったのを修正。

[v0.0.1.2]
・官能スライダーを使えるようにしました。
・iniファイルの設定項目にStatus、FaceAnime、FaceBlendを追加。
・FaceAnime、FaceBlendがEnabledの時、スキル変更時にこれらの設定を継続するようにしました。
・Lipsync cancellingが夜伽終了後も継続する不具合を修正。
・AutoKUPAの対象スキルを追加・修正しました。

[v0.0.0.1]
・互換モードに対応。
・メイド部屋など一部のステージで正常に機能しない不具合を修正（背景選択を一時的に無効化）。
・眼球の上下を変更するとAutoAHEで瞳が上昇し続ける、または瞬時に下降する不具合を修正。
・iniファイルのコメントが増殖する不具合を修正。
・Visual Studioを使わなくてもコンパイルできるようにソースコードを変更。
　（CoreUtil.csとLogger.csは改変元作者様の汎用クラスのようなので外しました。）

■既知の不具合など
・稀に機能しない時がある。
　→FadeInWaitの判定がうまくいっていない？ スキルを変更すれば動くようです。
・スキル変更時にAutoAHEの瞳の位置が維持されない。
　→仕様です。アニメーション開始時に瞳がリセットされるのでこうしないと不自然。何とかしたいのですが…
・感度スライダー（互換モード）がない。
　→後日対応予定？

■インストール
COM3D2.AddYotogiSlider.Plugin、COM3D2.AddYotogiSliderOld.Pluginを使用している場合は
これらをアンインストールしてください。

UnityInjectorフォルダ内のCOM3D2.AddYotogiSliderSE.Plugin.dllとConfigフォルダを
Sybaris\UnityInjectorに入れてください。

■アンインストール
COM3D2.AddYotogiSliderSE.Plugin.dllとSybaris\UnityInjector\Configファルダ内の
AddYotogiSliderSE.iniを削除してください。

■使い方
夜伽中に「F5」キーでGUIが表示されます。
詳しい使い方はCM3D2版のものを参照してください。
https://github.com/machinerie/CM3D2.AddYotogiSlider.Plugin

■設定ファイルについて
Status、FaceAnime、FaceBlendは「EnableOnLoad」を「True」に設定することで
夜伽開始時常に有効化することができます。
これらのセクション項目は「EnableOnLoad」が「True」の場合にのみ反映されます。
他のセクションとは違い、ゲーム内での変更は保存されません。
（「EnableOnLoad」を「False」のままにしておけば、これまでと同じように使えます。）

興奮と官能の値は固定の場合にのみ反映されます。
表情名、表情ブレンド名はソースファイルの「sFaceNames」からコピペしてください。

■謝辞
改変元の作者様とCM3D2版の作者様、並びにModderの皆様に深く感謝申し上げます。

■注意
以下の注意書きの範囲で改変および再配布は自由にしていただいてかまいません。

注意書き
※MODはKISSサポート対象外です。
※MODを利用するに当たり、問題が発生してもKISSは一切の責任を負いかねます。
※「カスタムメイド3D2」又は「カスタムオーダーメイド3D2」を購入されている方のみが利用できます。
※「カスタムメイド3D2」又は「カスタムオーダーメイド3D2」上で表示する目的以外の利用は禁止します。
※これらの事項は http://kisskiss.tv/kiss/diary.php?no=558 を優先します。