from tensorflow.keras.models import Sequential
from tensorflow.keras.layers import LSTM, Dense
from tensorflow.keras.callbacks import TensorBoard
import numpy as np
import os
from sklearn.model_selection import train_test_split
from tensorflow.keras.utils import to_categorical

moodel_file = "./model/static/"

log_dir = os.path.join('Logs')
tb_callback = TensorBoard(log_dir=log_dir)

no_sequences = 60
# Videos are going to be 30 frames in length
#static
sequence_length = 10
DATA_PATH = os.path.join('MP_Data')

# static action
actions = np.array(['IndexFinger_Right', 'IndexFinger_Forward', 'OpenHand_Right', 'OpenHand_Forward'])


# actions = np.array(['IndexFinger_LeftFlick', 'IndexFinger_RightFlick', 'OpenHand_LeftFlick', 'OpenHand_RightFlick', 'SnapFinger'])
# actions = np.array(['IndexFinger_LeftFlick', 'IndexFinger_RightFlick', 'IndexFinger_UpFlick', 'IndexFinger_DownFlick', 'OpenHand_LeftFlick', 'OpenHand_RightFlick', 'SnapFinger'])



# actions = np.array(['IndexFinger_LeftFlick', 'IndexFinger_RightFlick', 'IndexFinger_UpFlick', 'IndexFinger_DownFlick', 'SnapFinger'])

epoch = 800

label_map = {label:num for num, label in enumerate(actions)}
sequences, labels = [], []
for action in actions:
    for sequence in range(no_sequences):
        window = []
        for frame_num in range(sequence_length):
            res = np.load(os.path.join(DATA_PATH, action, str(sequence), "{}.npy".format(frame_num)))
            window.append(res)
        sequences.append(window)
        labels.append(label_map[action])

X = np.array(sequences)
y = to_categorical(labels).astype(int)
X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.05)

model = Sequential()
model.add(LSTM(64, return_sequences=True, activation='relu', input_shape=(sequence_length, 126)))
model.add(LSTM(128, return_sequences=True, activation='relu'))
model.add(LSTM(64, return_sequences=False, activation='relu'))
model.add(Dense(64, activation='relu'))
model.add(Dense(32, activation='relu'))
model.add(Dense(actions.shape[0], activation='softmax'))
model.compile(optimizer='Adam', loss='categorical_crossentropy', metrics=['categorical_accuracy'])
model.fit(X_train, y_train, epochs=epoch, callbacks=[tb_callback])
model.summary()
model.save(moodel_file + 'action' + str(epoch) + "_" + str(sequence_length) + "_" + str(len(actions)) + '.h5')
