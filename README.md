# BS-CameraMovement

このBeatSaberプラグインは、公式譜面エディタ にカメラワーク機能を追加、[ChroMapper-CameraMovement](https://github.com/rynan4818/ChroMapper-CameraMovement)のOSCを受信してエディタとプレイ画面で同期するプラグインです。

外部スクリプトファイル (`SongScript.json`) を読み込むことで、楽曲の再生に合わせてエディタ上のカメラを自動で動かすことができます。

ChroMapper-CameraMovementのプレビュー画面としてBeatSaber本体の実際の画面が使用できます。

<img width="1282" height="832" alt="image" src="https://github.com/user-attachments/assets/4fcc3f68-17ac-4166-89d5-33692747612f" />

<img width="1282" height="752" alt="image" src="https://github.com/user-attachments/assets/fe98fb10-2e50-463d-8c17-b33c9915975f" />

## 機能

*   譜面フォルダ内の `SongScript.json` に基づき、エディタ上のメインカメラを制御します。
*   エディタ起動中に `SongScript.json` を編集・保存すると、自動的に検知して再読み込みを行い、変更を即座に反映します。
*   チェックボックスでBS-CameraMovementの機能の有効/無効を切り替えられます。無効にするとカメラはデフォルトの位置,FOV,画面サイズに戻ります。
*   現在のカメラの位置、回転、視野角（FOV）をリアルタイムに表示・編集できます。
*   カメラの座標データをクリップボード経由でコピー＆ペーストできます（ScriptMapperの `q_` フォーマットにも対応）。
*   [ChroMapper-CameraMovement](https://github.com/rynan4818/ChroMapper-CameraMovement)のOSCを受信してカメラ位置角度FOVと再生位置が同期します。

## インストール

1.  [リリースページ](https://github.com/rynan4818/BS-CameraMovement/releases)から`BS-CameraMovement`のリリースをダウンロードします。
2.  ダウンロードしたzipファイルを`Beat Saber`フォルダに解凍して、`Plugins` フォルダに`BS-CameraMovement.dll`を配置してください。
    *   前提として`BSIPA`と`SiraUtil`が導入されている必要があります。

## 使い方

### カメラスクリプト再生
1.  公式エディタはブックマークや`SongScript.json`などの非公式なデータを削除してしまうことがあるため、今まで通りカメラスクリプト作成は`CustomWIPLevels`フォルダに置いた譜面をChroMapperで作成します。
2. `CustomLevels`フォルダに、`CustomWIPLevels`で編集中の譜面フォルダをコピーします。（同名のフォルダ名を作成します）
3. ScriptMapperは`CustomLevels`フォルダに同名のフォルダがあると、そちらにも `SongScript.json`をコピーしてくれます。
4. Beat Saber エディタで`CustomLevels`フォルダのマップを開きます。(公式エディタは`CustomLevels`フォルダの譜面しか表示できないと思います)
5. 画面上の "Show Camera UI" ボタンを押してウィンドウを表示し、"Enable CameraMovement" にチェックが入っていることを確認します。
6. タイムラインを再生すると、スクリプトに従ってカメラが動きます。
7. ScriptMapperで`SongScript.json`を更新すると、BS-CameraMovementが自動で検知してカメラスクリプトを読み込み直しします。

### ChroMapper-CameraMovement同期
* エディタ画面でChroMapper-CameraMovementのOSC Senderを有効にすると、一時停止時にカメラ位置角度FOVと再生位置が同期します。
* Practiceモードのプレイ画面で`Shift + F6`で同期機能のON/OFFが出来ます。プレイ中はカメラ位置角度FOV、一時停止状態では更に再生位置が同期します。

## 注意点
**`CustomLevels`フォルダの譜面を公式譜面エディタで開くとv4フォーマットに変換されるなど、譜面のID(ハッシュ値)が変わってしまい別の譜面になってしまいます。ScoreSaberやBeatLeaderなどのスコア送信ができなくなるので注意してください。譜面フォルダ名をデフォルトの物から変更することをオススメします**

### UI 操作 (エディタ画面)
<img width="306" height="256" alt="image" src="https://github.com/user-attachments/assets/9b7add5e-76cd-41a7-a576-5d4fc6e96208" />

*   **Show/Hide Camera UI**: 画面右上のボタンで設定ウィンドウの表示/非表示を切り替えます。
*   **Enable CameraMovement**: 機能のON/OFFを切り替えます。OFFにすると通常のカメラ操作に戻ります。
*   **Copy / Paste**:
    *   **Copy**: 現在のカメラ情報をテキストとしてクリップボードにコピーします。
    *   **Paste**: クリップボードのテキスト情報を読み取り、カメラに適用します。
    *   **q_format**: チェックを入れると、コピー時のフォーマットが `q_x_y_z_rx_ry_rz_fov` 形式になります（チェックを外すとタブ区切り形式）。
*   **Player OSC Receiver**: プレイ画面でのOSC同期機能のON/OFF状態です。

### UI 操作 (プレイ画面)
<img width="193" height="69" alt="image" src="https://github.com/user-attachments/assets/7dcfcaf0-7dae-486e-9da3-6dcef5c2cd88" />

*   **Disable(Shift+F6)**: 同期機能をOFFします。UI表示中はOSC同期機能がONです。Shift+F6でON/OFF切替できます。

## プラグイン製作の参考
`Components/CameraMovement.cs`ファイルは、すのーさんの[CameraPlus](https://github.com/Snow1226/CameraPlus)のソースコードをコピー・修正して使用しています。カメラ移動部分の処理は全く同じです。

CameraPlusの著作権表記・ライセンスは以下の通りです。
- https://github.com/Snow1226/CameraPlus/blob/master/LICENSE

再生箇所のシーク処理は、denpadokeiさんの[PracticePlugin](https://github.com/denpadokei/PracticePlugin)のソースコードをコピー・修正して使用しています。

PracticePluginの著作権表記・ライセンスは以下の通りです。
- https://github.com/denpadokei/PracticePlugin/blob/master/LICENSE
