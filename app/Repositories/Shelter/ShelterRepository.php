<?php

namespace App\Repositories\Shelter;

use App\Models\Shelter as ShelterModel;
use Illuminate\Database\Eloquent\Collection;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\ModelNotFoundException;

class ShelterRepository implements ShelterRepositoryInterface
{
    private ShelterModel $model;

    public function __construct(ShelterModel $model)
    {
        $this->model = $model;
    }

    public function getById($id): ShelterModel|Collection|Model|null
    {
        return $this->model->find($id);
    }

    public function getAll(): Collection
    {
        return $this->model->all();
    }

    public function create(array $data): ShelterModel
    {
        $shelter = $this->model->create($data);
        $shelter->address()->create($data['address']);

        return $shelter;
    }

    public function update($id, array $data): ShelterModel|Model|null
    {
        $shelter = $this->model->find($id);
        if ($shelter) {
            $shelter->update($data);
            $shelter->address->update($data['address']);

            return $shelter;
        }
        throw new ModelNotFoundException('Shelter not found');
    }
}
